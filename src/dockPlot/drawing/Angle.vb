Namespace ligplus
    Friend Class Angle
        Public Const RADDEG As Double = 57.295779513082323R

        Private Shared directionAngle As Double

        Public Shared Function calcAngle(x1 As Single, y1 As Single, x2 As Single, y2 As Single) As Double
            Dim x As Double = x2 - x1
            Dim y As Double = y2 - y1
            If Math.Abs(x) > 0.0001R Then
                directionAngle = 57.295779513082323R * Math.Atan(y / x)
                If directionAngle >= 0.0R Then
                    If x < 0.0R AndAlso y <= 0.0R Then
                        directionAngle += 180.0R
                    End If
                Else
                    directionAngle += 360.0R
                    If y > 0.0R Then
                        directionAngle -= 180.0R
                    End If
                End If
            ElseIf Math.Abs(y) < 0.0001R Then
                directionAngle = 0.0R
            ElseIf y > 0.0R Then
                directionAngle = 90.0R
            Else
                directionAngle = 270.0R
            End If
            Return directionAngle
        End Function

        Public Shared Function calcRotMatrix(angle As Double) As Double()()
            Dim matrix = {New Double(1) {}, New Double(1) {}}
            Dim theta = angle / 57.295779513082323R
            matrix(0)(0) = Math.Cos(theta)
            matrix(0)(1) = Math.Sin(theta)
            matrix(1)(0) = -Math.Sin(theta)
            matrix(1)(1) = Math.Cos(theta)
            Return matrix
        End Function

        Public Shared Function getInverseMatrix(matrix As Double()()) As Double()()
            Dim inverseMatrix = {New Double(1) {}, New Double(1) {}}
            inverseMatrix(0)(0) = matrix(0)(0)
            inverseMatrix(0)(1) = -matrix(0)(1)
            inverseMatrix(1)(0) = -matrix(1)(0)
            inverseMatrix(1)(1) = matrix(1)(1)
            Return inverseMatrix
        End Function

        Public Shared Function applyRotationMatrix(pivotX As Single, pivotY As Single, coords As Single(), matrix As Double()()) As Single()
            Dim newCoords = New Single(1) {}
            Dim x = coords(0) - pivotX
            Dim y = coords(1) - pivotY
            newCoords(0) = CSng(x * matrix(0)(0) + y * matrix(0)(1))
            newCoords(1) = CSng(x * matrix(1)(0) + y * matrix(1)(1))
            newCoords(0) = newCoords(0) + pivotX
            newCoords(1) = newCoords(1) + pivotY
            Return newCoords
        End Function

        Public Shared Function flipCoords(coords As Single(), shiftX As Single, shiftY As Single, matrix As Double()(), inverseMatrix As Double()()) As Single()
            coords(0) = coords(0) - shiftX
            coords(1) = coords(1) - shiftY
            coords = applyRotationMatrix(0.0F, 0.0F, coords, matrix)
            coords(1) = -coords(1)
            coords = applyRotationMatrix(0.0F, 0.0F, coords, inverseMatrix)
            coords(0) = coords(0) + shiftX
            coords(1) = coords(1) + shiftY
            Return coords
        End Function
    End Class

End Namespace
