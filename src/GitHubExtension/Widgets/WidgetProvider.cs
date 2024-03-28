﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets.Providers;

namespace GitHubExtension.Widgets;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[Guid("F23870B0-B391-4466-84E2-42A991078613")]
public sealed class WidgetProvider : IWidgetProvider, IWidgetProvider2
{
    public WidgetProvider()
    {
        Log.Logger()?.ReportDebug("Provider Constructed");
        widgetDefinitionRegistry.Add("GitHub_Issues", new WidgetImplFactory<GitHubIssuesWidget>());
        widgetDefinitionRegistry.Add("GitHub_PullRequests", new WidgetImplFactory<GitHubPullsWidget>());
        widgetDefinitionRegistry.Add("GitHub_MentionedIns", new WidgetImplFactory<GitHubMentionedInWidget>());
        widgetDefinitionRegistry.Add("GitHub_Assigneds", new WidgetImplFactory<GitHubAssignedWidget>());
        widgetDefinitionRegistry.Add("GitHub_Reviews", new WidgetImplFactory<GitHubReviewWidget>());
        widgetDefinitionRegistry.Add("GitHub_Releases", new WidgetImplFactory<GitHubReleasesWidget>());
        RecoverRunningWidgets();
    }

    ~WidgetProvider()
    {
        Log.Logger()?.Dispose();
    }

    private readonly Dictionary<string, IWidgetImplFactory> widgetDefinitionRegistry = new();
    private readonly Dictionary<string, WidgetImpl> runningWidgets = new();

    private void InitializeWidget(WidgetContext widgetContext, string state)
    {
        var widgetId = widgetContext.Id;
        var widgetDefinitionId = widgetContext.DefinitionId;
        Log.Logger()?.ReportDebug($"Calling Initialize for Widget Id: {widgetId} - {widgetDefinitionId}");
        if (widgetDefinitionRegistry.TryGetValue(widgetDefinitionId, out var widgetDefinition))
        {
            if (!runningWidgets.ContainsKey(widgetId))
            {
                var factory = widgetDefinition;
                var widgetImpl = factory.Create(widgetContext, state);
                runningWidgets.Add(widgetId, widgetImpl);
            }
            else
            {
                Log.Logger()?.ReportWarn($"Attempted to initialize a widget twice: {widgetDefinitionId} - {widgetId}");
            }
        }
        else
        {
            Log.Logger()?.ReportError($"Unknown widget DefinitionId: {widgetDefinitionId}");
        }
    }

    private void RecoverRunningWidgets()
    {
        WidgetInfo[] runningWidgets;
        try
        {
            runningWidgets = WidgetManager.GetDefault().GetWidgetInfos();
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError("Failed retrieving list of running widgets.", e);
            return;
        }

        if (runningWidgets is null)
        {
            Log.Logger()?.ReportDebug("No running widgets to recover.");
            return;
        }

        foreach (var widgetInfo in runningWidgets)
        {
            if (!this.runningWidgets.ContainsKey(widgetInfo.WidgetContext.Id))
            {
                InitializeWidget(widgetInfo.WidgetContext, widgetInfo.CustomState);
            }
        }

        Log.Logger()?.ReportDebug("Finished recovering widgets.");
    }

    public void CreateWidget(WidgetContext widgetContext)
    {
        Log.Logger()?.ReportInfo($"CreateWidget id: {widgetContext.Id} definitionId: {widgetContext.DefinitionId}");
        InitializeWidget(widgetContext, string.Empty);
    }

    public void Activate(WidgetContext widgetContext)
    {
        Log.Logger()?.ReportDebug($"Activate id: {widgetContext.Id} definitionId: {widgetContext.DefinitionId}");
        var widgetId = widgetContext.Id;
        if (runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.Activate(widgetContext);
        }
        else
        {
            // Called to activate a widget that we don't know about, which is unexpected. Try to recover by creating it.
            Log.Logger()?.ReportWarn($"Found WidgetId that was not known: {widgetContext.Id}, attempting to recover by creating it.");
            CreateWidget(widgetContext);
            if (runningWidgets.TryGetValue(widgetId, out var newWidget))
            {
                newWidget.Activate(widgetContext);
            }
        }
    }

    public void Deactivate(string widgetId)
    {
        Log.Logger()?.ReportDebug($"Deactivate id: {widgetId}");
        if (runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.Deactivate(widgetId);
        }
    }

    public void DeleteWidget(string widgetId, string customState)
    {
        Log.Logger()?.ReportInfo($"DeleteWidget id: {widgetId}");
        if (runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.DeleteWidget(widgetId, customState);
            runningWidgets.Remove(widgetId);
        }
    }

    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        Log.Logger()?.ReportDebug($"OnActionInvoked id: {actionInvokedArgs.WidgetContext.Id} definitionId: {actionInvokedArgs.WidgetContext.DefinitionId}");
        var widgetContext = actionInvokedArgs.WidgetContext;
        var widgetId = widgetContext.Id;
        if (runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.OnActionInvoked(actionInvokedArgs);
        }
    }

    public void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs)
    {
        Log.Logger()?.ReportDebug($"OnCustomizationRequested id: {customizationRequestedArgs.WidgetContext.Id} definitionId: {customizationRequestedArgs.WidgetContext.DefinitionId}");
        var widgetContext = customizationRequestedArgs.WidgetContext;
        var widgetId = widgetContext.Id;
        if (runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.OnCustomizationRequested(customizationRequestedArgs);
        }
    }

    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        Log.Logger()?.ReportDebug($"OnWidgetContextChanged id: {contextChangedArgs.WidgetContext.Id} definitionId: {contextChangedArgs.WidgetContext.DefinitionId}");
        var widgetContext = contextChangedArgs.WidgetContext;
        var widgetId = widgetContext.Id;
        if (runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.OnWidgetContextChanged(contextChangedArgs);
        }
    }
}
