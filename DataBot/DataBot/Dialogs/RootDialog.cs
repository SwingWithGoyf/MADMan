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
        protected List<string> slicerValues = new List<string>();
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
            message.Text = $"Let me get the MAD for you...";
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

                context.Call(new RefinementPickerDialog(filterValues, slicerValues, true), this.ResumeAfterFilterDialog);
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

                context.Call(new RefinementPickerDialog(filterValues, slicerValues, true), this.ResumeAfterFilterDialog);
            }
        }

        [LuisIntent("show.dad")]
        public async Task ShowDad(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;
            message.Text = $"Let me get the DAD for you...";
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

                context.Call(new RefinementPickerDialog(filterValues, slicerValues, true), this.ResumeAfterFilterDialog);
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

                context.Call(new RefinementPickerDialog(filterValues, slicerValues, true), this.ResumeAfterFilterDialog);
            }
        }

        public async Task<bool> HandleQuitAsync(IDialogContext context)
        {
            var message = context.Activity as IMessageActivity;

            var allowedQuitCommands = CustomQuitForm.GetAllowedQuitCommands();

            foreach (var command in allowedQuitCommands)
            {
                if (message.Text.Equals(command, StringComparison.InvariantCultureIgnoreCase))
                {
                    context.Wait(MessageReceived);
                    var msg = $"Conversation ended.  Start a new conversation with a question like 'show me the MAD for vr' or 'Show me DAD for hololens'";
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
                await context.PostAsync("Hi! Try asking me things like 'show me the MAD for vr' or 'Show me DAD for hololens'");
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
                    await LoopFilterSlicerAsync(context);
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

        /// <summary>
        /// This method is called when a filter choice is made (passes back a FilterResult, namely a filter name and a filter value)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task ResumeAfterFilterDialog(IDialogContext context, IAwaitable<FilterResult> result)
        {
            try
            {
                FilterResult currentFilterChoice = await result;
                if (currentFilterChoice.filterName.Equals(FilterPickerDialog.slicerSwitchText, StringComparison.OrdinalIgnoreCase))
                {
                    context.Call(new RefinementPickerDialog(filterValues, slicerValues, false, false), this.ResumeAfterSlicerDialog);
                }
                else if (currentFilterChoice.filterName.Equals(SlicerPickerDialog.filterSwitchText, StringComparison.OrdinalIgnoreCase))
                {
                    // special case: user picks the following in first run: slicers > switch to filters
                    context.Call(new RefinementPickerDialog(filterValues, slicerValues, false, true), this.ResumeAfterFilterDialog);
                }
                else if (string.IsNullOrEmpty(currentFilterChoice.filterValue))
                {
                    // special case: user picks slicer in the first run, filter value will be blank
                    await ResumeAfterSlicerDialog(context, result);
                }
                else
                {
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
                    await context.PostAsync(filterString);

                    await context.PostAsync("Please wait while we apply the filters.");

                    DeviceType dt = string.Equals(context.UserData.GetValue<string>(deviceTypeKey), "oasis", StringComparison.OrdinalIgnoreCase) ? DeviceType.Oasis : DeviceType.Hololens;

                    var newValue = string.Equals(context.UserData.GetValue<string>(metricTypeKey), "mad", StringComparison.OrdinalIgnoreCase) ? SSASTabularModel.GetMadNumber(filterValues, dt) : SSASTabularModel.GetDadNumber(filterValues, dt);

                    var resultMessage = context.MakeMessage();

                    resultMessage.Text = $"The new value is {newValue}";

                    await context.PostAsync(resultMessage);

                    await LoopFilterSlicerAsync(context);
                }
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
            }
        }

        private async Task ResumeAfterSlicerDialog(IDialogContext context, IAwaitable<FilterResult> result)
        {
            try
            {
                FilterResult currentFilterChoice = await result;
                if (currentFilterChoice.filterName.Equals(SlicerPickerDialog.filterSwitchText, StringComparison.OrdinalIgnoreCase))
                {
                    context.Call(new RefinementPickerDialog(filterValues, slicerValues, false, true), this.ResumeAfterFilterDialog);
                }
                else
                {
                    if (!slicerValues.Contains(currentFilterChoice.filterName))
                    {
                        slicerValues.Add(currentFilterChoice.filterName);
                    }
                    else
                    {
                        slicerValues.Remove(currentFilterChoice.filterName);
                        await context.PostAsync($"Removing slicer {currentFilterChoice.filterName}");
                    }

                    string slicerString = "Your current slicers are: ";
                    foreach (var slicer in slicerValues)
                    {
                        slicerString += $" slicer = {slicer}" + Environment.NewLine;
                    }
                    await context.PostAsync(slicerString);

                    await context.PostAsync("Please wait while we apply the slicers.");

                    //TODO: insert code to take list of slicers, and return back data in the form

                    // Dictionary<csv string of slicers, int>

                    //DeviceType dt = string.Equals(context.UserData.GetValue<string>(deviceTypeKey), "oasis", StringComparison.OrdinalIgnoreCase) ? DeviceType.Oasis : DeviceType.Hololens;

                    //var newValue = string.Equals(context.UserData.GetValue<string>(metricTypeKey), "mad", StringComparison.OrdinalIgnoreCase) ? SSASTabularModel.GetMadNumber(filterValues, dt) : SSASTabularModel.GetDadNumber(filterValues, dt);

                    //START TEMP PLACEHOLDER CODE
                    Dictionary<string, int> groupByData = new Dictionary<string, int>()
                        {
                            { "Canada,Acer,true", 14 },
                            { "Canada,Acer,false", 57 },
                            { "Canada,Samsung,true", 77 },
                            { "Canada,Samsung,false", 277 },
                            { "China,Acer,true", 140 },
                            { "China,Acer,false", 57 },
                            { "China,Samsung,true", 77 },
                            { "China,Samsung,false", 277 },
                            { "India,Acer,true", 14 },
                            { "India,Acer,false", 57 },
                            { "India,Samsung,true", 77 },
                            { "India,Samsung,false", 277 },
                            { "Argentina,Acer,true", 14 },
                            { "Argentina,Acer,false", 57 },
                            { "Argentina,Samsung,true", 77 },
                            { "Argentina,Samsung,false", 277 }
                        };

                    var tableResponse = BuildGroupByTable(
                        new List<string>() { "Country", "Manufacturer", "IsMainstream" },
                        groupByData);

                    //END TEMP PLACEHOLDER CODE

                    var resultMessage = context.MakeMessage();

                    resultMessage.Text = $"{tableResponse}";

                    await context.PostAsync("The data with the requested grouping is: ");
                    await context.PostAsync(resultMessage);
                    await LoopFilterSlicerAsync(context, false);
                }
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
            }

        }

        private string BuildGroupByTable(List<string> headers, Dictionary<string, int> groupByData)
        {
            // see markdown reference here: https://github.com/adam-p/markdown-here/wiki/Markdown-Cheatsheet#tables
            if (groupByData.Count <= 0)
            {
                // expect at least one header row and one or more data rows
                return string.Empty;
            }
            else
            {
                string tableString = string.Empty;

                // first build the header row
                tableString = "| ";
                foreach (string columnName in headers)
                {
                    tableString += columnName + " |";
                }
                tableString += " Value |" + Environment.NewLine;

                tableString += "| ";
                foreach (string columnName in headers)
                {
                    tableString += "--- |";
                }
                tableString += " --- |" + Environment.NewLine;

                // then add the body rows
                foreach (var groupByColumns in groupByData.Keys)
                {
                    tableString += "| ";
                    foreach (var cellValue in groupByColumns.Split(','))
                    {
                        tableString += cellValue + " |";
                    }

                    tableString += $" {groupByData[groupByColumns]} |" + Environment.NewLine;
                }
                return tableString;
            }
        }

        private async Task LoopFilterSlicerAsync(IDialogContext context, bool isFilterDialog = true)
        {
            context.Call(new RefinementPickerDialog(filterValues, slicerValues, false, isFilterDialog), this.ResumeAfterFilterDialog);
        }

        private async Task ResumeAfterOasisMadDialog(IDialogContext context, IAwaitable<FilterResult> result)
        {
            var isQuit = await HandleQuitAsync(context);

            if (!isQuit)
            {
                try
                {
                    await LoopFilterSlicerAsync(context);
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
                    await LoopFilterSlicerAsync(context);
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
                    await LoopFilterSlicerAsync(context);
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