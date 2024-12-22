// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Mercury.PowerShell.Utility;

/// <summary>
///   Represents a proxy for a PowerShell cmdlet.
/// </summary>
/// <param name="targetCmdlet">The target cmdlet to be proxied.</param>
[ExcludeFromCodeCoverage]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public abstract class PSCmdletProxy(string targetCmdlet) : PSCmdlet, IDynamicParameters, IDisposable {
  /// <summary>
  ///   The steppable pipeline of the cmdlet.
  /// </summary>
  protected SteppablePipeline? SteppablePipeline { get; set; }

  /// <inheritdoc />
  public void Dispose() {
    GC.SuppressFinalize(this);
    SteppablePipeline?.Dispose();
  }

  /// <inheritdoc />
  public object GetDynamicParameters() {
    var commandInfo = InvokeCommand.GetCommand(targetCmdlet, CommandTypes.Cmdlet, MyInvocation.BoundParameters.Values.ToArray());
    var dynamicParameters = commandInfo.Parameters.Where(pair => pair.Value.IsDynamic).ToList();

    if (dynamicParameters.Count == 0) {
      return new object();
    }

    var runtimeDefinedParameterDictionary = new RuntimeDefinedParameterDictionary();

    foreach (var (key, parameterMetadata) in dynamicParameters) {
      if (MyInvocation.MyCommand.Parameters.ContainsKey(key)) {
        continue;
      }

      var runtimeDefinedParameter = new RuntimeDefinedParameter(parameterMetadata.Name, parameterMetadata.ParameterType,
        parameterMetadata.Attributes);
      runtimeDefinedParameterDictionary.Add(parameterMetadata.Name, runtimeDefinedParameter);
    }

    return OnGetDynamicParameters(runtimeDefinedParameterDictionary);
  }

  /// <summary>
  ///   Method to be called when the cmdlet is initialized.
  /// </summary>
  /// <param name="runtimeDefinedParameterDictionary">The runtime defined parameters dictionary.</param>
  /// <returns>The runtime defined parameters dictionary.</returns>
  protected virtual object OnGetDynamicParameters(RuntimeDefinedParameterDictionary runtimeDefinedParameterDictionary)
    => runtimeDefinedParameterDictionary;

  /// <summary>
  ///   The parameters to pass when processing the <see cref="SteppablePipeline" />.
  /// </summary>
  /// <returns>A object with the parameters.</returns>
  protected virtual object PipelineDefinedParameters()
    => GetParameters(this);

  /// <summary>
  ///   Action to be executed together with <see cref="BeginProcessing" /> method.
  /// </summary>
  protected virtual void OnBeginProcessing() { }

  /// <summary>
  ///   Action to be executed together with <see cref="ProcessRecord" /> method.
  /// </summary>
  protected virtual void OnProcessRecord() { }

  /// <summary>
  ///   Action to be executed together with <see cref="EndProcessing" /> method.
  /// </summary>
  protected virtual void OnEndProcessing() { }

  /// <inheritdoc />
  protected sealed override void BeginProcessing() {
    if (MyInvocation.BoundParameters.TryGetValue("OutBuffer", out var _)) {
      MyInvocation.BoundParameters["OutBuffer"] = 1;
    }

    var commandInfo = InvokeCommand.GetCommand(targetCmdlet, CommandTypes.Cmdlet);
    var scriptBlock = ScriptBlock.Create("& $CommandInfo @BoundParameters @UnboundArguments");
    SessionState.PSVariable.Set("CommandInfo", commandInfo);
    SessionState.PSVariable.Set("UnboundArguments", MyInvocation.UnboundArguments);
    SessionState.PSVariable.Set("BoundParameters", MyInvocation.BoundParameters);

    SteppablePipeline = scriptBlock.GetSteppablePipeline(MyInvocation.CommandOrigin);

    Parallel.Invoke(() => SteppablePipeline.Begin(this), OnBeginProcessing);
  }

  /// <inheritdoc />
  protected sealed override void ProcessRecord()
    => Parallel.Invoke(() => SteppablePipeline?.Process(PipelineDefinedParameters()), OnProcessRecord);

  /// <inheritdoc />
  protected sealed override void EndProcessing()
    => Parallel.Invoke(() => SteppablePipeline?.End(), OnEndProcessing);

  /// <inheritdoc />
  protected sealed override void StopProcessing()
    => Dispose();

  /// <summary>
  ///   Get the parameters of a cmdlet.
  /// </summary>
  /// <param name="cmdlet">The cmdlet.</param>
  /// <typeparam name="TCmdlet">The type of the cmdlet.</typeparam>
  /// <returns>A <see cref="PSObject" /> with the parameters of the cmdlet.</returns>
  protected static PSObject GetParameters<TCmdlet>(TCmdlet cmdlet) where TCmdlet : PSCmdlet {
    var psobject = new PSObject();
    var properties = typeof(TCmdlet)
      .GetProperties()
      .Where(property => property.GetCustomAttribute<ParameterAttribute>() is not null)
      .Select(property => new PSNoteProperty(property.Name, property.GetValue(cmdlet)))
      .ToArray();

    foreach (var property in properties) {
      psobject.Properties.Add(property);
    }

    return psobject;
  }

  /// <summary>
  ///   Get a parameter from a cmdlet.
  /// </summary>
  /// <param name="cmdlet">The cmdlet.</param>
  /// <param name="parameter">The parameter capturing function.</param>
  /// <typeparam name="TCmdlet">The type of the cmdlet.</typeparam>
  /// <typeparam name="TOut">The type of the parameter.</typeparam>
  /// <returns>The parameter.</returns>
  protected static TOut GetParameter<TCmdlet, TOut>(TCmdlet cmdlet, Func<TCmdlet, TOut> parameter) where TCmdlet : PSCmdlet
    => parameter(cmdlet);
}
