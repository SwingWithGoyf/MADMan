using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

#pragma warning disable 1998

namespace DataBot.Dialogs
{
    [Serializable]
    public class SlicerPickerDialog : IDialog<string>
    {
        const string slicerText = " (slicer applied)";

        public const string filterSwitchText = "Switch to add filters";

        protected List<string> slicerValues;

        public SlicerPickerDialog(List<string> slicerValues)
        {
            this.slicerValues = slicerValues;
        }

        public async Task StartAsync(IDialogContext context)
        {
            List<string> modifiedSlicerOptions = new List<string>();
            foreach (var slicer in RefinementPickerDialog.filterOptions)
            {
                if (slicerValues.Contains(slicer))
                {
                    string modifiedFilter =  $"{slicer}{slicerText}";
                    modifiedSlicerOptions.Add(modifiedFilter);
                }
                else
                {
                    modifiedSlicerOptions.Add(slicer);
                }
            }
            modifiedSlicerOptions.Add(filterSwitchText);
            modifiedSlicerOptions.Add("Quit");

            string promptText = "slicers";

            PromptDialog.Choice<string>(
                context: context,
                resume: AfterSlicerChoiceAsync,
                options: modifiedSlicerOptions,
                prompt: $"Please select from the options below to apply additional {promptText}",
                retry: "Sorry, didn't understand that input - try 'help' or 'quit'",
                attempts: 3,
                promptStyle: PromptStyle.Auto,
                descriptions: null
            );
        }

        public async Task AfterSlicerChoiceAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var filter = await argument;
            var isQuit = await new RootLuisDialog().HandleQuitAsync(context);

            if (!isQuit)
            {
                context.Done(filter);
            }
        }
    }
}