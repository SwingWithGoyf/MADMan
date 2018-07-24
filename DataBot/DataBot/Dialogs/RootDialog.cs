namespace DataBot.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.FormFlow;
    using Microsoft.Bot.Builder.FormFlow.Advanced;
    using Microsoft.Bot.Builder.Luis;
    using Microsoft.Bot.Builder.Luis.Models;
    using Microsoft.Bot.Connector;
    using DataBot;

#pragma warning disable CS1998

    [LuisModel("fe229bbe-aa41-4102-974f-bc8ac03315e3", "8dcf9c66b84f4a959687955c6f92d36f")]
    [Serializable]
    public class RootLuisDialog : LuisDialog<object>
    {
        private const string DeviceTypeEntity = "DeviceType";
        private static EntityRecommendation deviceType;
        protected Dictionary<string, object> filterValues = new Dictionary<string, object>();
        protected string choice = string.Empty;
        const string deviceTypeKey = "DeviceType";
        const string metricTypeKey = "MetricType";

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = $"Sorry, I did not understand '{result.Query}'. Type 'help' if you need assistance.";

            await context.PostAsync(message);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("show.mad")]
        public async Task ShowMad(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;
            message.Text = $"Let us get the MAD for you...";
            await context.PostAsync(message.Text);

            if (result.TryFindEntity(DeviceTypeEntity, out deviceType))
            {
                deviceType.Type = "DeviceType";
            }

            if (string.Equals(deviceType.Entity, "vr", StringComparison.OrdinalIgnoreCase) || string.Equals(deviceType.Entity, "oasis", StringComparison.OrdinalIgnoreCase))
            {
                var mad = SSASTabularModel.GetMadNumber(new Dictionary<string, object>(), DeviceType.Oasis);

                var resultMessage = context.MakeMessage();

                resultMessage.Text = $"Mad for Oasis is {mad}";

                context.UserData.Clear();

                context.UserData.SetValue(deviceTypeKey, "Oasis");
                context.UserData.SetValue(metricTypeKey, "mad");

                await context.PostAsync(resultMessage);

                context.Call(new FilterPickerDialog(filterValues), this.ResumeAfterFilterPickerDialog);
            }
            else
            {
                var mad = SSASTabularModel.GetMadNumber(new Dictionary<string, object>(), DeviceType.Hololens);

                var resultMessage = context.MakeMessage();

                resultMessage.Text = $"Mad for Hololens is {mad}";

                context.UserData.Clear();

                context.UserData.SetValue(deviceTypeKey, "Hololens");
                context.UserData.SetValue(metricTypeKey, "mad");

                await context.PostAsync(resultMessage);

                context.Call(new FilterPickerDialog(filterValues), this.ResumeAfterFilterPickerDialog);
            }
        }

        [LuisIntent("show.dad")]
        public async Task ShowDad(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;
            message.Text = $"Let us get the DAD for you...";
            await context.PostAsync(message.Text);

            if (result.TryFindEntity(DeviceTypeEntity, out deviceType))
            {
                deviceType.Type = "DeviceType";
            }

            if (string.Equals(deviceType.Entity, "vr", StringComparison.OrdinalIgnoreCase) || string.Equals(deviceType.Entity, "oasis", StringComparison.OrdinalIgnoreCase))
            {
                var dad = SSASTabularModel.GetDadNumber(new Dictionary<string, object>(), DeviceType.Oasis);

                var resultMessage = context.MakeMessage();

                context.UserData.Clear();

                context.UserData.SetValue(deviceTypeKey, "Oasis");
                context.UserData.SetValue(metricTypeKey, "dad");

                resultMessage.Text = $"Dad for Oasis is {dad}";

                await context.PostAsync(resultMessage);

                context.Call(new FilterPickerDialog(filterValues), this.ResumeAfterFilterPickerDialog);
            }
            else
            {
                var dad = SSASTabularModel.GetDadNumber(new Dictionary<string, object>(), DeviceType.Hololens);

                var resultMessage = context.MakeMessage();

                context.UserData.Clear();

                context.UserData.SetValue(deviceTypeKey, "Hololens");
                context.UserData.SetValue(metricTypeKey, "dad");

                resultMessage.Text = $"Dad for Hololens is {dad}";

                await context.PostAsync(resultMessage);

                context.Call(new FilterPickerDialog(filterValues), this.ResumeAfterFilterPickerDialog);
            }
        }

        private async Task<bool> HandleQuitAsync(IDialogContext context)
        {
            var message = context.Activity as IMessageActivity;

            var allowedQuitCommands = CustomQuitForm.GetAllowedQuitCommands();

            foreach (var command in allowedQuitCommands)
            {
                if (message.Text.Equals(command, StringComparison.InvariantCultureIgnoreCase))
                {
                    context.Wait(MessageReceived);
                    var msg = $"Sorry you quit the dialog. How may I help you?";
                    await context.PostAsync(msg);
                    return true;
                }
            }

            return false;
        }

        [LuisIntent("Help")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            var isQuit = await HandleQuitAsync(context);

            if (!isQuit)
            {
                await context.PostAsync("Hi! Try asking me things like 'show me the MAD for vr', 'Show me DAD for hololens'");
                context.Wait(this.MessageReceived);
            }
        }

        private async Task ResumeAfterHololensMadDialog(IDialogContext context, IAwaitable<FilterResult> result)
        {
            var isQuit = await HandleQuitAsync(context);

            if (!isQuit)
            {
                try
                {
                    await LoopFilterAsync(context);
                }
                catch (FormCanceledException ex)
                {
                    string reply;

                    if (ex.InnerException == null)
                    {
                        reply = "You have canceled the operation.";
                    }
                    else
                    {
                        reply = $"Oops! Something went wrong :( Technical Details: {ex.InnerException.Message}";
                    }

                    await context.PostAsync(reply);
                }
            }
        }

        private async Task ResumeAfterFilterPickerDialog(IDialogContext context, IAwaitable<FilterResult> result)
        {
            try
            {
                try
                {
                    FilterResult currentFilterChoice = await result;

                    if (!filterValues.ContainsKey(currentFilterChoice.filterName) && !currentFilterChoice.filterValue.Equals("clear", StringComparison.OrdinalIgnoreCase))
                    {
                        filterValues.Add(currentFilterChoice.filterName, currentFilterChoice.filterValue);
                    }
                    else if (currentFilterChoice.filterValue.Equals("clear", StringComparison.OrdinalIgnoreCase))
                    {
                        filterValues.Remove(currentFilterChoice.filterName);
                        await context.PostAsync($"Removing filter {currentFilterChoice.filterName}");
                    }
                    else
                    {
                        filterValues[currentFilterChoice.filterName] = currentFilterChoice.filterValue;
                    }

                    string filterString = "Your current filters are: ";
                    foreach (var filter in filterValues.Keys)
                    {
                        filterString += $" filter = {filter}, value = {filterValues[filter]}" + Environment.NewLine;
                    }

                    await context.PostAsync("Please wait while we apply the filters.");

                    DeviceType dt = string.Equals(context.UserData.GetValue<string>(deviceTypeKey), "oasis", StringComparison.OrdinalIgnoreCase) ? DeviceType.Oasis : DeviceType.Hololens;

                    var newValue = string.Equals(context.UserData.GetValue<string>(metricTypeKey), "mad", StringComparison.OrdinalIgnoreCase) ? SSASTabularModel.GetMadNumber(filterValues, dt) : SSASTabularModel.GetDadNumber(filterValues, dt);

                    var resultMessage = context.MakeMessage();

                    resultMessage.Text = $"The new value is {newValue}";

                    await context.PostAsync(resultMessage);
                }
                catch (TooManyAttemptsException)
                {
                    await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
                }
            }
            finally
            {
                await LoopFilterAsync(context);
            }
        }

        private async Task LoopFilterAsync(IDialogContext context)
        {
            context.Call(new FilterPickerDialog(filterValues), this.ResumeAfterFilterPickerDialog);
        }

        private async Task ResumeAfterOasisMadDialog(IDialogContext context, IAwaitable<FilterResult> result)
        {
            var isQuit = await HandleQuitAsync(context);

            if (!isQuit)
            {
                try
                {
                    await LoopFilterAsync(context);
                }
                catch (FormCanceledException ex)
                {
                    string reply;

                    if (ex.InnerException == null)
                    {
                        reply = "You have canceled the operation.";
                    }
                    else
                    {
                        reply = $"Oops! Something went wrong :( Technical Details: {ex.InnerException.Message}";
                    }

                    await context.PostAsync(reply);
                }
            }
        }

        private async Task ResumeAfterHololensDadDialog(IDialogContext context, IAwaitable<FilterResult> result)
        {
            var isQuit = await HandleQuitAsync(context);

            if (!isQuit)
            {
                try
                {
                    await LoopFilterAsync(context);
                }
                catch (FormCanceledException ex)
                {
                    string reply;

                    if (ex.InnerException == null)
                    {
                        reply = "You have canceled the operation.";
                    }
                    else
                    {
                        reply = $"Oops! Something went wrong :( Technical Details: {ex.InnerException.Message}";
                    }

                    await context.PostAsync(reply);
                }
            }
        }

        private async Task ResumeAfterOasisDadDialog(IDialogContext context, IAwaitable<FilterResult> result)
        {
            var isQuit = await HandleQuitAsync(context);

            if (!isQuit)
            {
                try
                {
                    await LoopFilterAsync(context);
                }
                catch (FormCanceledException ex)
                {
                    string reply;

                    if (ex.InnerException == null)
                    {
                        reply = "You have canceled the operation.";
                    }
                    else
                    {
                        reply = $"Oops! Something went wrong :( Technical Details: {ex.InnerException.Message}";
                    }

                    await context.PostAsync(reply);
                }
            }

        }
    }
}