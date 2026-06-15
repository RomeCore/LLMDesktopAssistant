using System.Linq;

using LLMDesktopAssistant.Prompting.Plugins;
using LLTSharp;
using LLTSharp.DataAccessors;
using LLTSharp.Metadata;
using LLTSharp.Metadata.Types;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	[LuaApi(chatScoped: true)]
	public class LuaApiTemplates : LuaApiBase
	{
		public override string? Namespace => "templates";

		public override string? Manuals => """
			--- templates — template rendering and inspection API

			Provides access to the template library for rendering and listing templates.
			Templates are identified by ID and can be filtered by metadata (language, model, etc.).

			FUNCTIONS:
			
			--- templates.import(templateString)
			  Imports one or more templates from a string (LLT format).
			  Parameters:
			    - templateString: string — LLT template source code.
			  Returns:
			    - number — number of imported templates.
			
			--- templates.render(filters, [context])
			  Renders a template from the template library.
			  Parameters:
			    - filters: string or table — Specifies which template to render.
			      If string: treated as a template ID (e.g. "system_prompt").
			      If table: supports the following keys:
			        - Numeric key: Template ID.
			        - "id": string — Explicit template ID.
			        - "lang": string — Language code (e.g. "en_US", "ru_RU", "uk_UA").
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

			--- templates.list([filters])
			  Lists all templates registered in the template library with their metadata.
			  Parameters:
			    - filters: table (optional) — Filter criteria. Supports:
			      - Numeric keys (1, 2, 3...): template IDs to filter by.
			      - "id": string — Template ID to filter by.
			      - "type": string — Template type: "text" or "messages".
			      - "lang": string — Language code.
			      - "model": string — Target model name.
			      - "model_family": string — Target model family.
			      - "version": string — Version string.
			      - Any other string key: custom metadata to filter by.
			  Returns:
			    - table — Array of template descriptor tables. Each entry contains:
			      - id: string — Template identifier.
			      - type: string — "text" or "messages".
			      - lang: string (optional) — Language code.
			      - model: string (optional) — Target model.
			      - model_family: string (optional) — Target model family.
			      - version: string (optional) — Version.
			      - ... plus any custom metadata keys flattened directly.
			
			NOTES:
			  - In templates.render(), templates are matched by intersecting all provided
			    metadata criteria. Each metadata filter narrows down the candidate set.
			  - If multiple templates match all filters, the first one is returned.
			  - Template functions from installed IPromptTemplatePlugins are
			    automatically available during rendering.

			EXAMPLES:
			
			  -- Import a new template from a LLT string
			  local count = templates.import([[
			    @template hello { Hello, @name! }
			  ]])
			  print("Imported " .. count .. " templates")

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

			  -- Render a messages template (returns array of messages)
			  local messages = templates.render("chat_messages", {
			    topic = "Hello World"
			  })
			  for _, msg in ipairs(messages) do
			    print(msg.role, msg.content)
			  end

			  -- List all templates
			  local all = templates.list()
			  for _, t in ipairs(all) do
			    print(t.id, t.type, t.lang)
			  end

			  -- List templates by language
			  local ru = templates.list({ lang = "ru_RU" })

			  -- List text templates only
			  local texts = templates.list({ type = "text" })

			  -- List by custom metadata
			  local experimental = templates.list({ my_custom_tag = "experimental" })
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
			ns["import"] = DynValue.NewCallback(Import);
			ns["render"] = DynValue.NewCallback(Render);
			ns["list"] = DynValue.NewCallback(List);
		}

		private DynValue Import(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("templates.import(templateString): at least 1 argument expected.");

			if (args[0].Type != DataType.String)
				throw new ScriptRuntimeException("templates.import(): first argument must be a string.");

			var templateString = args[0].String;

			try
			{
				var beforeCount = _templateLibrary.Count();
				_templateLibrary.ImportFromString(templateString, languageCode: "llt");
				var afterCount = _templateLibrary.Count();
				return DynValue.NewNumber(afterCount - beforeCount);
			}
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"templates.import(): {ex.Message}");
			}
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
								case "id":
									id = value;
									break;

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
					return DynValue.NewTable(messagesTable);
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

		private DynValue List(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var script = ctx.GetScript();

			// Parse optional filter
			Dictionary<string, string>? stringFilters = null;
			string? typeFilter = null;
			string? idFilter = null;

			if (args.Count > 0 && args[0].Type == DataType.Table)
			{
				stringFilters = new Dictionary<string, string>();
				foreach (var kvp in args[0].Table.Pairs)
				{
					if (kvp.Key.Type == DataType.Number)
					{
						if (kvp.Value.Type != DataType.String)
							throw new ScriptRuntimeException("templates.list(): filter table values for number keys must be strings.");
						idFilter = kvp.Value.String;
					}
					else if (kvp.Key.Type == DataType.String)
					{
						if (kvp.Value.Type != DataType.String)
							throw new ScriptRuntimeException("templates.list(): filter table values for string keys must be strings.");

						var key = kvp.Key.String.ToLowerInvariant();
						var value = kvp.Value.String;

						if (key == "id")
							idFilter = value;
						else if (key == "type")
							typeFilter = value;
						else
							stringFilters[key] = value;
					}
					else
					{
						throw new ScriptRuntimeException("templates.list(): filter table keys must be numbers or strings.");
					}
				}
			}

			var result = new Table(script);
			int index = 1;

			foreach (var template in _templateLibrary)
			{
				// Check type filter
				if (typeFilter != null)
				{
					string type = template is ITextTemplate ? "text"
						: template is IMessagesTemplate ? "messages"
						: "unknown";
					if (!string.Equals(type, typeFilter, StringComparison.OrdinalIgnoreCase))
						continue;
				}

				// Check ID filter
				if (idFilter != null)
				{
					var tmplIdMeta = template.GetMetadata<TemplateIdentifierMetadata>();
					if (tmplIdMeta == null || !string.Equals(tmplIdMeta.Identifier, idFilter, StringComparison.OrdinalIgnoreCase))
						continue;
				}

				// Check metadata string filters
				if (stringFilters != null && stringFilters.Count > 0)
				{
					if (!TemplateMatchesFilters(template, stringFilters))
						continue;
				}

				// Build entry table
				var entry = new Table(script);

				// ID
				var idMeta = template.GetMetadata<TemplateIdentifierMetadata>();
				if (idMeta != null)
					entry["id"] = DynValue.NewString(idMeta.Identifier);

				// Type
				if (template is ITextTemplate)
					entry["type"] = DynValue.NewString("text");
				else if (template is IMessagesTemplate)
					entry["type"] = DynValue.NewString("messages");

				// Language
				var langMeta = template.GetMetadata<LanguageMetadata>();
				if (langMeta != null)
					entry["lang"] = DynValue.NewString(langMeta.LanguageCode.ToString());

				// Target model
				var modelMeta = template.GetMetadata<TargetModelMetadata>();
				if (modelMeta != null)
					entry["model"] = DynValue.NewString(modelMeta.ModelName);

				// Target model family
				var familyMeta = template.GetMetadata<TargetModelFamilyMetadata>();
				if (familyMeta != null)
					entry["model_family"] = DynValue.NewString(familyMeta.FamilyName);

				// Version
				var versionMeta = template.GetMetadata<VersionMetadata>();
				if (versionMeta != null)
					entry["version"] = DynValue.NewString(versionMeta.Version.ToString());

				// Additional custom metadata — flattened directly into the entry
				foreach (var additionalMeta in template.GetAllMetadata<AdditionalMetadata>())
				{
					if (entry.Keys.Contains(DynValue.NewString(additionalMeta.Key)))
						continue; // skip if already set by known metadata field

					entry[additionalMeta.Key] = ObjectToDynValue(script, additionalMeta.Value);
				}

				result[index++] = DynValue.NewTable(entry);
			}

			return DynValue.NewTable(result);
		}

		private static bool TemplateMatchesFilters(ITemplate template, Dictionary<string, string> filters)
		{
			foreach (var filter in filters)
			{
				var key = filter.Key;
				var value = filter.Value;

				switch (key)
				{
					case "lang":
						{
							var lang = template.GetMetadata<LanguageMetadata>();
							if (lang == null || !string.Equals(lang.LanguageCode.ToString(), value, StringComparison.OrdinalIgnoreCase))
								return false;
							break;
						}

					case "model":
						{
							var model = template.GetMetadata<TargetModelMetadata>();
							if (model == null || !string.Equals(model.ModelName, value, StringComparison.OrdinalIgnoreCase))
								return false;
							break;
						}

					case "model_family":
						{
							var family = template.GetMetadata<TargetModelFamilyMetadata>();
							if (family == null || !string.Equals(family.FamilyName, value, StringComparison.OrdinalIgnoreCase))
								return false;
							break;
						}

					case "version":
						{
							var version = template.GetMetadata<VersionMetadata>();
							if (version == null || !string.Equals(version.Version.ToString(), value, StringComparison.OrdinalIgnoreCase))
								return false;
							break;
						}

					default:
						{
							// Custom metadata — check AdditionalMetadata
							var customMetas = template.GetAllMetadata<AdditionalMetadata>();
							bool found = false;
							foreach (var customMeta in customMetas)
							{
								if (customMeta.Key == key)
								{
									var metaVal = customMeta.Value;
									if (string.Equals(metaVal?.ToString(), value, StringComparison.OrdinalIgnoreCase))
									{
										found = true;
										break;
									}
								}
							}
							if (!found)
								return false;
							break;
						}
				}
			}
			return true;
		}

		private static DynValue ObjectToDynValue(Script script, object? value)
		{
			if (value == null)
				return DynValue.Nil;

			if (value is string s)
				return DynValue.NewString(s);
			if (value is bool b)
				return DynValue.NewBoolean(b);
			if (value is int i)
				return DynValue.NewNumber(i);
			if (value is long l)
				return DynValue.NewNumber(l);
			if (value is double d)
				return DynValue.NewNumber(d);
			if (value is float f)
				return DynValue.NewNumber(f);
			if (value is decimal m)
				return DynValue.NewNumber((double)m);

			return DynValue.NewString(value.ToString() ?? "");
		}
	}
}
