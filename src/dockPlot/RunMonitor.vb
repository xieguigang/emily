
Namespace ligplus

    Public Class RunMonitor
        Public Shared [CONTINUE] As Integer = 0

        Public Shared CANCEL As Integer = 1

        Public Shared START As Integer = 0

        Public Shared RUNNING_HBADD As Integer = 1

        Public Shared RUNNING_HBPLUS1 As Integer = 2

        Public Shared RUNNING_HBPLUS2 As Integer = 3

        Public Shared RUNNING_DIMER As Integer = 4

        Public Shared RUNNING_LIGPLOT As Integer = 5

        Public Shared NSTAGES As Integer = 6

        Public Shared progressPercent As Integer()() = New Integer()() {New Integer() {0, 10, 40, 70, 70, 100}, New Integer() {0, 10, 30, 60, 90, 100}}

        Public Shared ReadOnly runMessage As String() = New String() {"Initialising ...", "Running HBADD ...", "Running HBPLUS: H-bonds ...", "Running HBPLUS: contacts ...", "Running DIMER ...", "Running LIGPLOT ..."}

        Private status As Integer = [CONTINUE]

        Private currentBar As Integer = 1

        Private program As Integer












    End Class

End Namespace
