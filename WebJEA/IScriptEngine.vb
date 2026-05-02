Imports System.Management.Automation

Public Interface IScriptEngine

    Property Script As String
    Property Parameters As Dictionary(Of String, Object)
    ReadOnly Property Runtime As Single

    Property LogParameters As Boolean
    Property Verbose As Boolean
    Property PipeToOutString As Boolean
    Property WebJEAUserName As String
    Property WebJEAHostName As String
    Property HasErrors As Boolean

    Function GetOutputData() As Queue(Of PSEngine.OutputData)
    Function GetOutputObjects() As List(Of PSObject)

    Sub Run()
    Sub AddParameter(Key As String, Value As Object)
    Sub RemoveParameter(Key As String)
    Sub UpdateParameter(Key As String, Value As Object)
    Sub ClearParameters()
    Sub ClearOutput()

End Interface
