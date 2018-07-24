using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

#pragma warning disable 1998

namespace DataBot.Dialogs
{
    [Serializable]
    public class FilterValueDialog : IDialog<string>
    {
        private string filterName;

        public FilterValueDialog(string filterName)
        {
            this.filterName = filterName;
        }

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync($"Please enter a filter value for {this.filterName}: (or 'clear' to remove this filter)");
            context.Wait(this.MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;
            string value = message.Text;
            context.Done(value);
        }
    }
}