Namespace models

    Public Class Conect

        Public Overridable ReadOnly Property Atom1 As Atom
        Public Overridable ReadOnly Property Atom2 As Atom

        Public Sub New(atom1 As Atom, atom2 As Atom)
            _Atom1 = atom1
            _Atom2 = atom2
        End Sub
    End Class
End Namespace
