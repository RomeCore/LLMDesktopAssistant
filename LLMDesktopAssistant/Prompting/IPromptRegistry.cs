using LLTSharp;

namespace LLMDesktopAssistant.Prompting
{
	public interface IPromptRegistry
	{
		TemplateLibrary SharedLibrary { get; }

		PromptComponent? GetComponent(Guid id);
		Persona? GetPersona(Guid id);
		BehaviourSlider? GetSlider(Guid id);
		Specialization? GetSpecialization(Guid id);
	}
}