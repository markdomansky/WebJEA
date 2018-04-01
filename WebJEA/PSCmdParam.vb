Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.ComponentModel

Public Class PSCmdParam

    Public Enum ParameterType
        PSString
        PSInt
        PSFloat
        PSDate
        PSBoolean
    End Enum

    Public Name As String
    Public HelpMessage As String = ""
    Public HelpDetail As String = ""
    Public DirectiveMultiline As Boolean = False
    Public DirectiveDateTime As Boolean = False
    Public VarType As String = ""
    Public DefaultValue As Object = Nothing
    'TODO: Add support for more than just string default values - can we support arrays?
    Public Validation As New List(Of String)
    'Private prvValidation As New List(Of PSCmdParamVal)

    Sub New()

    End Sub

    Public ReadOnly Property IsMandatory As Boolean
        Get
            For Each val As String In Validation
                If val.ToUpper = "MANDATORY" Then
                    'parameter is required
                    Return True
                End If
            Next
            Return False
        End Get
    End Property

    Public ReadOnly Property ParamType As ParameterType
        Get
            Dim vartypestr = VarType.ToLower
            If vartypestr Like "string*" Then
                Return ParameterType.PSString
            ElseIf vartypestr = "datetime" Then
                Return ParameterType.PSDate
            ElseIf vartypestr Like "single*" Or vartypestr Like "double*" Or vartypestr Like "float*" Then
                Return ParameterType.PSFloat
            ElseIf vartypestr Like "bool*" Or vartypestr Like "switch" Then
                Return ParameterType.PSBoolean
            ElseIf vartypestr Like "int*" Or vartypestr Like "uint*" Or vartypestr Like "byte*" Or vartypestr Like "long*" Then
                Return ParameterType.PSInt
            End If
            'by default we treat a value as string.  This includes PSCredential
            Return ParameterType.PSString
        End Get
    End Property

    Public ReadOnly Property IsMultiValued As Boolean
        Get
            If VarType.Contains("[]") Then
                Return True
            ElseIf DirectiveMultiline Then
                Return False
            End If
            Return False
        End Get
    End Property

    Public ReadOnly Property AllowedValues As List(Of String)
        Get
            'this param does not explicitly define allowed values
            If IsSelect = False Then Return Nothing
            For Each valobj As PSCmdParamVal In ValidationObjects
                If valobj.Type = PSCmdParamVal.ValType.SetCol Then
                    Return valobj.Options
                End If
            Next

            'should not have gotten here
            Return Nothing
        End Get
    End Property

    Public ReadOnly Property IsSelect As Boolean
        Get
            For Each valobj As PSCmdParamVal In ValidationObjects
                If valobj.Type = PSCmdParamVal.ValType.SetCol Then
                    'parameter is restricted
                    Return True
                End If
            Next
            'no validateset found
            Return False
        End Get
    End Property

    Public ReadOnly Property ValidationObjects As List(Of PSCmdParamVal)
        Get
            Dim retobjs As New List(Of PSCmdParamVal)
            For Each rule As String In Validation
                Dim obj As New PSCmdParamVal(rule)
                If obj.IsValid Then
                    retobjs.Add(obj)
                End If
            Next
            Return retobjs

        End Get
    End Property

    Public Sub AddValidation(valstring As String)
        'TODO add support for properly managing conflicting validation options
        If valstring.ToUpper.StartsWith("VALIDATE") Or valstring.ToUpper.StartsWith("ALLOW") Or valstring.ToUpper.StartsWith("MANDATORY") Then
            If Not Validation.Contains(valstring) Then
                'don't add precise duplicates.  Doesn't stop from adding incompatible validation commands
                Validation.Add(valstring)
            End If
        ElseIf valstring.ToUpper.StartsWith("ALIAS") Then
            'do nothing
        Else 'variable
            dlog.Warn("Unexpected Validation Type not supported: " & valstring)
        End If

    End Sub

    Public Function Clone() As PSCmdParam
        Dim psparam As New PSCmdParam
        psparam.Name = Name
        psparam.HelpMessage = HelpMessage
        psparam.HelpDetail = HelpDetail
        psparam.VarType = VarType
        psparam.DirectiveDateTime = DirectiveDateTime
        psparam.DirectiveMultiline = DirectiveMultiline
        psparam.DefaultValue = DefaultValue
        For Each val As String In Validation
            psparam.AddValidation(val)
        Next

        Return psparam

    End Function

    Public Sub MergeUnder(psparam As PSCmdParam)
        'this will merge "under" the current parameter.
        'it will NOT overwrite properties (Help, etc), but if there is no value specified, it will add the value

        If String.IsNullOrWhiteSpace(HelpMessage) Then HelpMessage = psparam.HelpMessage
        If String.IsNullOrWhiteSpace(HelpDetail) Then HelpDetail = psparam.HelpDetail
        If String.IsNullOrWhiteSpace(VarType) Then VarType = psparam.VarType
        If String.IsNullOrWhiteSpace(DefaultValue) Then DefaultValue = psparam.DefaultValue
        For Each valstr As String In psparam.Validation
            AddValidation(valstr)
        Next

    End Sub
    Public Sub MergeOver(psparam As PSCmdParam)
        'this will merge "over" the current parameter.
        'it WILL overwrite properties (Help, etc), if specified
        'validation is always merge

        If Not String.IsNullOrWhiteSpace(psparam.HelpMessage) Then HelpMessage = psparam.HelpMessage
        If Not String.IsNullOrWhiteSpace(psparam.HelpDetail) Then HelpDetail = psparam.HelpDetail
        If Not String.IsNullOrWhiteSpace(psparam.VarType) Then VarType = psparam.VarType
        If Not String.IsNullOrWhiteSpace(psparam.DefaultValue) Then DefaultValue = psparam.DefaultValue
        For Each valstr As String In psparam.Validation
            AddValidation(valstr)
        Next

    End Sub

    Public ReadOnly Property FieldName As String
        Get
            Return "psparam_" & Name
        End Get
    End Property

End Class
