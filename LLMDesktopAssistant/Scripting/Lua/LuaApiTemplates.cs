using System;
using System.Collections.Generic;
using System.Text;
using LLMDesktopAssistant.Prompting.Plugins;
using LLTSharp;
using LLTSharp.DataAccessors;
using LLTSharp.Metadata;
using LLTSharp.Metadata.Factories;
using LLTSharp.Metadata.Types;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	[LuaApi(chatScoped: true)]
	public class LuaApiTemplates : LuaApiBase
	{
		public override string? Namespace => "templates";

		public override string? Manuals => """
			--- templates — template rendering API

			Provides access to the template library for rendering text and message templates.
			Templates are identified by ID and can be filtered by metadata (language, model, etc.).

			FUNCTIONS:

			--- templates.render(filters, [context])
			  Renders a template from the template library.
			  Parameters:
			    - filters: string or table — Specifies which template to render.
			      If string: treated as a template ID (e.g. "system_prompt").
			      If table: supports the following keys:
			        - One numeric key: template ID.
			        - "lang": string — Language code (e.g. "en_US", "ru_RU", "fr_FR", etc.).
			        - "model": string — Target model name.
			        - "model_family": string — Target model family.
			        - "version": string — Version string (e.g. "1.0.0").
			        - Any other string key: treated as additional custom metadata.
			    - context: any (optional) — Data context for template variables.
			      Accepts nil, boolean, number, string, or table.
			      Tables with sequential integer keys become arrays.
			      Tables with string/non-sequential keys become objects/dictionaries.
			  Returns:
			    - string — for text templates.
			    - table — for message templates:
			      Returns an array of { role: string, content: string } tables.

			NOTES:
			  - Templates are matched by intersecting all provided metadata criteria.
			    Each metadata filter narrows down the candidate set.
			  - If multiple templates match all filters, the first one is returned.
			  - Numeric keys (IDs) act as a fallback: the first existing ID is used.
			  - The context object is converted to LLTSharp TemplateDataAccessor:
			    - Lua nil → null accessor
			    - Lua boolean → TemplateBooleanAccessor
			    - Lua number → TemplateNumberAccessor
			    - Lua string → TemplateStringAccessor
			    - Lua table (array) → TemplateArrayAccessor
			    - Lua table (object) → TemplateDictionaryAccessor
			  - Template functions from installed IPromptTemplatePlugins are
			    automatically available during rendering.

			EXAMPLES:

			  -- Render by template ID (string filter)
			  local prompt = templates.render("system_prompt")
			  print(prompt)

			  -- Render with language filter
			  local prompt = templates.render({
			    "system_prompt",
			    lang = "ru_RU"
			  })
			  print(prompt)

			  -- Render with context data
			  local prompt = templates.render("greeting", {
			    name = "Alice",
			    age = 30,
			    tags = {"admin", "user"}
			  })

			  -- Fallback chain: try "specialized_prompt", fallback to "default_prompt"
			  local prompt = templates.render({
			    "specialized_prompt",
			    "default_prompt",
			    lang = "en_US"
			  })

			  -- Render a messages template (returns array of messages)
			  local messages = templates.render("chat_messages", {
			    topic = "Hello World"
			  })
			  for _, msg in ipairs(messages) do
			    print(msg.role, msg.content)
			  end

			  -- Multiple metadata filters: language + model family
			  local prompt = templates.render({
			    "system_prompt",
			    lang = "en_US",
			    model_family = "gpt4"
			  })

			  -- Custom additional metadata
			  local prompt = templates.render({
			    "custom_prompt",
			    my_custom_tag = "experimental"
			  })
			""";

		private readonly TemplateLibrary _templateLibrary;
		private readonly IEnumerable<IPromptTemplatePlugin> _promptTemplatePlugins;

		public LuaApiTemplates(TemplateLibrary templateLibrary, IEnumerable<IPromptTemplatePlugin> promptTemplatePlugins)
		{
			_templateLibrary = templateLibrary;
			_promptTemplatePlugins = promptTemplatePlugins;
		}

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["render"] = DynValue.NewCallback(Render);
		}

		private DynValue Render(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("templates.render(filters, [context]): at least 1 argument expected.");

			var filters = args[0];
			if (filters.Type != DataType.String && filters.Type != DataType.Table)
				throw new ScriptRuntimeException("templates.render(): first argument must be a string or a table.");

			TemplateDataAccessor context = TemplateNullAccessor.Instance;
			if (args.Count > 1)
				context = StructuredLuaConverter.DynValueToLLTSharp(args[1]);

			var script = ctx.GetScript();

			try
			{
				ITemplate template;
				var functions = new TemplateFunctionSet(_promptTemplatePlugins.SelectMany(p => p.GetTemplateFunctions()));

				if (filters.Type == DataType.String)
				{
					template = _templateLibrary.Retrieve(filters.String);
				}
				else
				{
					string? id = null;
					var metadatas = new List<IMetadata>();

					foreach (var kvp in filters.Table.Pairs)
					{
						if (kvp.Key.Type == DataType.Number)
						{
							if (kvp.Value.Type != DataType.String)
								throw new ScriptRuntimeException("templates.render(): filter table values for number keys must be strings.");

							id = kvp.Value.String;
						}
						else if (kvp.Key.Type == DataType.String)
						{
							if (kvp.Value.Type != DataType.String)
								throw new ScriptRuntimeException("templates.render(): filter table values for string keys must be strings.");
							
							var key = kvp.Key.String.ToLowerInvariant();
							var value = kvp.Value.String;
							switch (key)
							{
								case "lang":
									metadatas.Add(new LanguageMetadata(value));
									break;

								case "model":
									metadatas.Add(new TargetModelMetadata(value));
									break;

								case "model_family":
									metadatas.Add(new TargetModelFamilyMetadata(value));
									break;

								case "version":
									metadatas.Add(new VersionMetadata(Version.Parse(value)));
									break;

								default:
									metadatas.Add(new AdditionalMetadata(key, value));
									break;
							}
						}
						else
						{
							throw new ScriptRuntimeException("templates.render(): filter table keys must be numbers or strings.");
						}
					}

					if (id != null)
						template = _templateLibrary.Retrieve(id, metadatas.ToArray());
					else
						template = _templateLibrary.Retrieve(metadatas.ToArray());
				}

				if (template is ITextTemplate textTemplate)
				{
					return DynValue.NewString(textTemplate.Render(context, functions));
				}
				else if (template is IMessagesTemplate messagesTemplate)
				{
					var messages = messagesTemplate.Render(context, functions);
					var messagesTable = new Table(script, messages.Select(m =>
					{
						return DynValue.NewTable(new Table(script)
						{
							["role"] = m.Role.ToString(),
							["content"] = m.Content
						});
					}).ToArray());
				}

				throw new ScriptRuntimeException($"templates.render(): not supported template type: {template?.GetType().FullName}.");
			}
			catch (ScriptRuntimeException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"templates.render(): {ex.Message}");
			}
		}
	}
}