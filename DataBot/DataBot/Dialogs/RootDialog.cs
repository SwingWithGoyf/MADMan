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
        protected bool isCurrentContextOasis = true;
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

        [LuisIntent("mad.measure")]
        public async Task CreateMadMeasure(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;
            message.Text = $"Let me create the measure for you...";
            await context.PostAsync(message.Text);

            if (result.TryFindEntity(DeviceTypeEntity, out deviceType))
            {
                deviceType.Type = "DeviceType";
            }

            if (string.Equals(deviceType.Entity, "vr", StringComparison.OrdinalIgnoreCase) || string.Equals(deviceType.Entity, "oasis", StringComparison.OrdinalIgnoreCase))
            {
                context.UserData.Clear();

                context.UserData.SetValue(deviceTypeKey, "Oasis");
                context.UserData.SetValue(metricTypeKey, "mad");

                context.Call(new FilterPickerDialog(filterValues), this.ResumeAfterFilterPickerDialogWithMadMeasure);
            }
            else
            {
                context.UserData.Clear();

                context.UserData.SetValue(deviceTypeKey, "Hololens");
                context.UserData.SetValue(metricTypeKey, "mad");

                context.Call(new FilterPickerDialog(filterValues, false), this.ResumeAfterFilterPickerDialogWithMadMeasure);
            }
        }

        [LuisIntent("dad.measure")]
        public async Task CreateDadMeasure(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;
            message.Text = $"Let me create the measure for you...";
            await context.PostAsync(message.Text);

            if (result.TryFindEntity(DeviceTypeEntity, out deviceType))
            {
                deviceType.Type = "DeviceType";
            }

            if (string.Equals(deviceType.Entity, "vr", StringComparison.OrdinalIgnoreCase) || string.Equals(deviceType.Entity, "oasis", StringComparison.OrdinalIgnoreCase))
            {
                context.UserData.Clear();

                context.UserData.SetValue(deviceTypeKey, "Oasis");
                context.UserData.SetValue(metricTypeKey, "dad");

                context.Call(new FilterPickerDialog(filterValues), this.ResumeAfterFilterPickerDialogWithDadMeasure);
            }
            else
            {
                context.UserData.Clear();

                context.UserData.SetValue(deviceTypeKey, "Hololens");
                context.UserData.SetValue(metricTypeKey, "dad");

                context.Call(new FilterPickerDialog(filterValues, false), this.ResumeAfterFilterPickerDialogWithDadMeasure);
            }
        }


        [LuisIntent("show.mad")]
        public async Task ShowMad(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;
            message.Text = $"Let me get the MAD for you...";
            await context.PostAsync(message.Text);

            //var text = "<table><tr><th>heading 1</th><th>heading 2</th></tr><tr><td>row 1 column 1</td><td>row 1 column 2</td></tr><tr><td>row 2 column 1</td><td>row 2 column 2</td></tr></table>";
            //await context.PostAsync(text);

            if (result.TryFindEntity(DeviceTypeEntity, out deviceType))
            {
                deviceType.Type = "DeviceType";
            }

            if (string.Equals(deviceType.Entity, "vr", StringComparison.OrdinalIgnoreCase) || string.Equals(deviceType.Entity, "oasis", StringComparison.OrdinalIgnoreCase))
            {
                isCurrentContextOasis = true;
                var mad = SSASTabularModel.GetMadNumber(new Dictionary<string, object>(), DeviceType.Oasis);

                var resultMessage = context.MakeMessage();

                resultMessage.Text = $"Mad for Oasis is **{mad}**";

                context.UserData.Clear();

                context.UserData.SetValue(deviceTypeKey, "Oasis");
                context.UserData.SetValue(metricTypeKey, "mad");

                await context.PostAsync(resultMessage);

                context.Call(new RefinementPickerDialog(filterValues, slicerValues, firstRun: true, showFilterDialog: true, isOasis: isCurrentContextOasis), this.ResumeAfterFilterDialog);
            }
            else
            {
                isCurrentContextOasis = false;
                var mad = SSASTabularModel.GetMadNumber(new Dictionary<string, object>(), DeviceType.Hololens);

                var resultMessage = context.MakeMessage();

                resultMessage.Text = $"Mad for Hololens is **{mad}**";

                context.UserData.Clear();

                context.UserData.SetValue(deviceTypeKey, "Hololens");
                context.UserData.SetValue(metricTypeKey, "mad");

                await context.PostAsync(resultMessage);

                context.Call(new RefinementPickerDialog(filterValues, slicerValues, firstRun: true, showFilterDialog: true, isOasis: isCurrentContextOasis), this.ResumeAfterFilterDialog);
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
                isCurrentContextOasis = true;
                var dad = SSASTabularModel.GetDadNumber(new Dictionary<string, object>(), DeviceType.Oasis);

                var resultMessage = context.MakeMessage();

                resultMessage.Text = $"Dad for Oasis is **{dad}**";

                context.UserData.Clear();

                context.UserData.SetValue(deviceTypeKey, "Oasis");
                context.UserData.SetValue(metricTypeKey, "dad");

                await context.PostAsync(resultMessage);

                context.Call(new RefinementPickerDialog(filterValues, slicerValues, firstRun: true, showFilterDialog: true, isOasis: isCurrentContextOasis), this.ResumeAfterFilterDialog);
            }
            else
            {
                isCurrentContextOasis = false;
                var dad = SSASTabularModel.GetDadNumber(new Dictionary<string, object>(), DeviceType.Hololens);

                var resultMessage = context.MakeMessage();

                resultMessage.Text = $"Dad for Hololens is **{dad}**";

                context.UserData.Clear();

                context.UserData.SetValue(deviceTypeKey, "Hololens");
                context.UserData.SetValue(metricTypeKey, "dad");

                await context.PostAsync(resultMessage);

                context.Call(new RefinementPickerDialog(filterValues, slicerValues, firstRun: true, showFilterDialog: true, isOasis: isCurrentContextOasis), this.ResumeAfterFilterDialog);
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
                    context.Call(new RefinementPickerDialog(filterValues, slicerValues, firstRun: false, showFilterDialog: false, isOasis: isCurrentContextOasis), this.ResumeAfterSlicerDialog);
                }
                else if (currentFilterChoice.filterName.Equals(SlicerPickerDialog.filterSwitchText, StringComparison.OrdinalIgnoreCase))
                {
                    // special case: user picks the following in first run: slicers > switch to filters
                    context.Call(new RefinementPickerDialog(filterValues, slicerValues, firstRun: false, showFilterDialog: true, isOasis: isCurrentContextOasis), this.ResumeAfterFilterDialog);
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
                        filterString += $" filter name = **{filter}**, fitler value = **{filterValues[filter]}**" + Environment.NewLine;
                    }
                    await context.PostAsync(filterString);

                    string slicerString = "Your current slicers are: ";
                    foreach (var slicer in slicerValues)
                    {
                        slicerString += $" slicer name = **{slicer}**" + Environment.NewLine;
                    }
                    await context.PostAsync(slicerString);

                    await context.PostAsync("Please wait while we apply the filters.");

                    DeviceType dt = string.Equals(context.UserData.GetValue<string>(deviceTypeKey), "oasis", StringComparison.OrdinalIgnoreCase) ? DeviceType.Oasis : DeviceType.Hololens;
                    string madDadValue = context.UserData.GetValue<string>(metricTypeKey).ToUpper();
                    IMessageActivity resultMessage;

                    if (slicerValues.Count > 0)
                    {
                        await context.PostAsync("One or more slicers selected, displaying results as a table...");
                        var groupByData = string.Equals(madDadValue, "mad", StringComparison.OrdinalIgnoreCase)
                            ? SSASTabularModel.ExecuteGroupByMad(slicerValues, filterValues, dt)
                            : SSASTabularModel.ExecuteGroupByDad(slicerValues, filterValues, dt);

                        var tableResponse = BuildGroupByTable(slicerValues, groupByData);

                        resultMessage = context.MakeMessage();

                        resultMessage.Text = $"The {madDadValue} data with the requested grouping is: {Environment.NewLine}{tableResponse}";
                    }
                    else
                    {
                        await context.PostAsync("No slicers selected, returning a single value...");

                        var newValue = string.Equals(madDadValue, "mad", StringComparison.OrdinalIgnoreCase)
                            ? SSASTabularModel.GetMadNumber(filterValues, dt)
                            : SSASTabularModel.GetDadNumber(filterValues, dt);

                        resultMessage = context.MakeMessage();

                        resultMessage.Text = $"The new {madDadValue} value is **{newValue}**";
                    }

                    await context.PostAsync(resultMessage);

                    // loop back and show filter/slicer selection again
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
                    context.Call(new RefinementPickerDialog(filterValues, slicerValues, firstRun: false, showFilterDialog: true, isOasis: isCurrentContextOasis), this.ResumeAfterFilterDialog);
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

                    string filterString = "Your current filters are: ";
                    foreach (var filter in filterValues.Keys)
                    {
                        filterString += $" filter = {filter}, value = {filterValues[filter]}" + Environment.NewLine;
                    }
                    await context.PostAsync(filterString);

                    string slicerString = "Your current slicers are: ";
                    foreach (var slicer in slicerValues)
                    {
                        slicerString += $" slicer = {slicer}" + Environment.NewLine;
                    }
                    await context.PostAsync(slicerString);

                    await context.PostAsync("Please wait while we apply the slicers.");

                    DeviceType dt = string.Equals(context.UserData.GetValue<string>(deviceTypeKey), "oasis", StringComparison.OrdinalIgnoreCase) ? DeviceType.Oasis : DeviceType.Hololens;
                    string madDadValue = context.UserData.GetValue<string>(metricTypeKey).ToUpper();
                    IMessageActivity resultMessage;

                    if (slicerValues.Count > 0)
                    {
                        await context.PostAsync("One or more slicers selected, displaying results as a table...");
                        var groupByData = string.Equals(madDadValue, "mad", StringComparison.OrdinalIgnoreCase)
                            ? SSASTabularModel.ExecuteGroupByMad(slicerValues, filterValues, dt)
                            : SSASTabularModel.ExecuteGroupByDad(slicerValues, filterValues, dt);

                        var tableResponse = BuildGroupByTable(slicerValues, groupByData);

                        resultMessage = context.MakeMessage();

                        resultMessage.Text = $"The {madDadValue} data with the requested grouping is: {Environment.NewLine}{tableResponse}";
                    }
                    else
                    {
                        await context.PostAsync("No slicers selected, returning a single value...");

                        var newValue = string.Equals(madDadValue, "mad", StringComparison.OrdinalIgnoreCase)
                            ? SSASTabularModel.GetMadNumber(filterValues, dt)
                            : SSASTabularModel.GetDadNumber(filterValues, dt);

                        resultMessage = context.MakeMessage();

                        resultMessage.Text = $"The new {madDadValue} value is **{newValue}**";
                    }

                    await context.PostAsync(resultMessage);

                    // loop back and show filters/slicers again
                    await LoopFilterSlicerAsync(context, false);
                }
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
            }

        }

        private string BuildGroupByTable(List<string> headers, SortedDictionary<string, double> groupByData)
        {
            // see markdown reference here: https://github.com/adam-p/markdown-here/wiki/Markdown-Cheatsheet#tables
            if (groupByData.Count <= 0)
            {
                // expect at least one header row and one or more data rows
                return string.Empty;
            }
            else
            {
                double totalSoFar = 0;
                string tableString = string.Empty;

                // first build the header row
                tableString = "<table cellpadding=1 cellspacing=1><span <tr><th> ";
                foreach (string columnName in headers)
                {
                    tableString += columnName + " </th><th>&nbsp;";
                }
                tableString += "&nbsp;Value </th></tr>" + Environment.NewLine;

                // then add the body rows
                foreach (var groupByColumns in groupByData.Keys)
                {
                    tableString += "<tr><td>";
                    foreach (var cellValue in groupByColumns.Split(','))
                    {
                        tableString += cellValue + " </td><td>&nbsp;";
                    }

                    tableString += $"&nbsp;{groupByData[groupByColumns]} </td></tr>" + Environment.NewLine;
                    totalSoFar += groupByData[groupByColumns];
                }

                // finally, add a total row
                tableString += "<tr><td> <b>TOTAL<b>";
                foreach (string columnName in headers)
                {
                    tableString += " </td><td>&nbsp;";
                }
                tableString += $"&nbsp;<b>{totalSoFar}</b> </td></tr></table>" + Environment.NewLine;
                return tableString;
            }
        }

        private async Task LoopFilterSlicerAsync(IDialogContext context, bool isFilterDialog = true)
        {
            context.Call(new RefinementPickerDialog(filterValues, slicerValues, firstRun: false, showFilterDialog: isFilterDialog, isOasis: isCurrentContextOasis), this.ResumeAfterFilterDialog);
        }

        private async Task LoopFilterAsyncWithMadMeasure(IDialogContext context)
        {
            context.Call(new FilterPickerDialog(filterValues, isCurrentContextOasis), this.ResumeAfterFilterPickerDialogWithMadMeasure);
        }

        private async Task LoopFilterAsyncWithDadMeasure(IDialogContext context)
        {
            context.Call(new FilterPickerDialog(filterValues, isCurrentContextOasis), this.ResumeAfterFilterPickerDialogWithDadMeasure);
        }

        private async Task ResumeAfterFilterPickerDialogWithMadMeasure(IDialogContext context, IAwaitable<FilterResult> result)
        {
            try
            {
                try
                {
                    var isQuit = await HandleQuitAsync(context);

                    if (!isQuit)
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

                        await context.PostAsync("Please wait while we apply the filters.");

                        DeviceType dt = string.Equals(context.UserData.GetValue<string>(deviceTypeKey), "oasis", StringComparison.OrdinalIgnoreCase) ? DeviceType.Oasis : DeviceType.Hololens;
                        isCurrentContextOasis = (dt == DeviceType.Oasis);

                        string madString = "MAD";

                        string measureName = string.Join(",", filterValues.Values);

                        SSASTabularModel.CreateNewMadMeasure($"{madString} - " + measureName, measureName, filterValues, dt);

                        var resultMessage = context.MakeMessage();

                        resultMessage.Text = $"New measure is created and the name of the measure is \"{madString} - {measureName}\".";

                        await context.PostAsync(resultMessage);
                    }
                }
                catch (TooManyAttemptsException)
                {
                    await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
                }
            }
            finally
            {
                await LoopFilterAsyncWithMadMeasure(context);
            }
        }

        private async Task ResumeAfterFilterPickerDialogWithDadMeasure(IDialogContext context, IAwaitable<FilterResult> result)
        {
            try
            {
                try
                {
                    var isQuit = await HandleQuitAsync(context);

                    if (!isQuit)
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

                        await context.PostAsync("Please wait while we apply the filters.");

                        DeviceType dt = string.Equals(context.UserData.GetValue<string>(deviceTypeKey), "oasis", StringComparison.OrdinalIgnoreCase) ? DeviceType.Oasis : DeviceType.Hololens;
                        isCurrentContextOasis = (dt == DeviceType.Oasis);

                        string dadString = "DAD";

                        string measureName = string.Join(",", filterValues.Values);

                        SSASTabularModel.CreateNewDadMeasure($"{dadString} - " + measureName, measureName, filterValues, dt);

                        var resultMessage = context.MakeMessage();

                        resultMessage.Text = $"New measure is created and the name of the measure is \"{dadString} - {measureName}\".";

                        await context.PostAsync(resultMessage);
                    }
                }
                catch (TooManyAttemptsException)
                {
                    await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
                }
            }
            finally
            {
                await LoopFilterAsyncWithDadMeasure(context);
            }
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