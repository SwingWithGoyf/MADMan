using Microsoft.Bot.Builder.FormFlow;
using System.Collections.Generic;
using System.Linq;

namespace DataBot.Dialogs
{
    public class CustomQuitForm
    {
        public static List<string> GetAllowedQuitCommands()
        {
            return new List<string> { "quit", "exit", "cancel" };
        }

        public static IFormBuilder<T> CreateCustomForm<T>() where T : class
        {
            var form = new FormBuilder<T>();
            var command = form.Configuration.Commands[FormCommand.Quit];
            var terms = command.Terms.ToList();

            var allowedQuitCommands = GetAllowedQuitCommands();
            foreach (var cmd in allowedQuitCommands)
            {
                terms.Add(cmd);
            }

            command.Terms = terms.ToArray();

            var templateAttribute = form.Configuration.Template(TemplateUsage.NotUnderstood);
            var patterns = templateAttribute.Patterns;
            patterns[0] += " Type *cancel* to quit or *help* if you want more information.";
            templateAttribute.Patterns = patterns;

            return form;
        }
    }
}