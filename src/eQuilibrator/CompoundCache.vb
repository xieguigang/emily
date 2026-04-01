Imports eQuilibrator.EquilibratorApi.Core.Constants
Imports eQuilibrator.EquilibratorApi.Core.Models

Namespace EquilibratorApi.Core
    ''' <summary>
    ''' A cache for compound data
    ''' </summary>
    Public Class CompoundCache
        Private ReadOnly _compounds As Dictionary(Of String, Compound) = New Dictionary(Of String, Compound)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _searchIndex As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))(StringComparer.OrdinalIgnoreCase)

        ''' <summary>
        ''' Gets the proton compound
        ''' </summary>
        Public ReadOnly Property Proton As Compound

        ''' <summary>
        ''' Gets the water compound
        ''' </summary>
        Public ReadOnly Property Water As Compound

        ''' <summary>
        ''' Creates a new CompoundCache with default compounds
        ''' </summary>
        Public Sub New()
            ' Add default compounds
            Proton = New Compound With {
        .Id = "H+",
        .Name = "H+",
        .Charge = 1,
        .ProtonCount = 1,
        .IsProton = True,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"H", 1}
            }
    }
            AddCompound(Proton)

            Water = New Compound With {
        .Id = "H2O",
        .Name = "water",
        .Charge = 0,
        .ProtonCount = 2,
        .MolecularWeight = 18.015,
        .IsWater = True,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"H", 2},
                {"O", 1}
            }
    }
            AddCompound(Water)

            ' Add some common metabolites
            AddCommonMetabolites()
        End Sub

        ''' <summary>
        ''' Adds a compound to the cache
        ''' </summary>
        ''' <paramname="compound">The compound to add</param>
        Public Sub AddCompound(compound As Compound)
            _compounds(compound.Id) = compound

            ' Add to search index
            AddToSearchIndex(compound.Id, compound.Id)
            If Not String.IsNullOrEmpty(compound.Name) Then
                Me.AddToSearchIndex(compound.Name, compound.Id)
            End If
        End Sub

        Private Sub AddToSearchIndex(key As String, compoundId As String)
            Dim normalizedKey = key.ToLowerInvariant()
            Dim list As List(Of String) = Nothing

            If Not _searchIndex.TryGetValue(normalizedKey, list) Then
                list = New List(Of String)()
                _searchIndex(normalizedKey) = list
            End If
            If Not list.Contains(compoundId) Then
                list.Add(compoundId)
            End If
        End Sub

        ''' <summary>
        ''' Gets a compound by its identifier
        ''' </summary>
        ''' <paramname="compoundId">The compound identifier</param>
        ''' <returns>The compound, or null if not found</returns>
        Public Function GetCompound(compoundId As String) As Compound
            Dim compound As Compound = Nothing
            Return If(_compounds.TryGetValue(compoundId, compound), compound, Nothing)
        End Function

        ''' <summary>
        ''' Gets a PhasedCompound by its identifier
        ''' </summary>
        ''' <paramname="compoundId">The compound identifier</param>
        ''' <paramname="phase">The phase (default: aqueous)</param>
        ''' <returns>The PhasedCompound, or null if not found</returns>
        Public Function GetPhasedCompound(compoundId As String, Optional phase As String = ThermodynamicConstants.DefaultPhase) As PhasedCompound
            Dim compound = GetCompound(compoundId)
            Return compound?.ToPhasedCompound(phase)
        End Function

        ''' <summary>
        ''' Searches for compounds by name or identifier
        ''' </summary>
        ''' <paramname="query">The search query</param>
        ''' <returns>A list of matching compounds</returns>
        Public Function SearchCompounds(query As String) As List(Of Compound)
            Dim normalizedQuery = query.ToLowerInvariant()
            Dim results = New List(Of Compound)()

            ' Exact match
            Dim exactMatches As List(Of String) = Nothing

            If _searchIndex.TryGetValue(normalizedQuery, exactMatches) Then
                results.AddRange(exactMatches.[Select](New Func(Of String, Compound)(AddressOf Me.GetCompound)).Where(Function(c) c IsNot Nothing))
            End If

            ' Partial match
            For Each i In _searchIndex
                Dim key = i.Key
                Dim compoundIds = i.Value

                If key.Contains(normalizedQuery) AndAlso Not exactMatches?.Contains(compoundIds.FirstOrDefault()) = True Then
                    results.AddRange(compoundIds.[Select](New Func(Of String, Compound)(AddressOf Me.GetCompound)).Where(Function(c) c IsNot Nothing))
                End If
            Next

            Return results.DistinctBy(Function(c) c.Id).ToList()
        End Function

        ''' <summary>
        ''' Checks if a compound is a proton
        ''' </summary>
        Public Function IsProton(compoundId As String) As Boolean
            Return compoundId.Equals("H+", StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Checks if a compound is water
        ''' </summary>
        Public Function IsWater(compoundId As String) As Boolean
            Return compoundId.Equals("H2O", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Sub AddCommonMetabolites()
            ' Add some common metabolites with approximate standard formation energies
            ' Note: These are approximate values for demonstration purposes

            Me.AddCompound(New Compound With {
        .Id = "ATP",
        .Name = "ATP",
        .Charge = -4,
        .StandardFormationEnergy = -2768.0, ' Approximate
        .MolecularWeight = 507.18,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"C", 10},
                {"H", 16},
                {"N", 5},
                {"O", 13},
                {"P", 3}
            }
    })

            Me.AddCompound(New Compound With {
        .Id = "ADP",
        .Name = "ADP",
        .Charge = -3,
        .StandardFormationEnergy = -1906.0, ' Approximate
        .MolecularWeight = 427.2,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"C", 10},
                {"H", 15},
                {"N", 5},
                {"O", 10},
                {"P", 2}
            }
    })

            Me.AddCompound(New Compound With {
        .Id = "AMP",
        .Name = "AMP",
        .Charge = -2,
        .StandardFormationEnergy = -1044.0, ' Approximate
        .MolecularWeight = 347.22,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"C", 10},
                {"H", 14},
                {"N", 5},
                {"O", 7},
                {"P", 1}
            }
    })

            Me.AddCompound(New Compound With {
        .Id = "Pi",
        .Name = "phosphate",
        .Charge = -2,
        .StandardFormationEnergy = -1096.0, ' Approximate
        .MolecularWeight = 95.98,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"H", 1},
                {"O", 4},
                {"P", 1}
            }
    })

            Me.AddCompound(New Compound With {
        .Id = "glucose",
        .Name = "D-glucose",
        .Charge = 0,
        .StandardFormationEnergy = -917.0, ' Approximate
        .MolecularWeight = 180.16,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"C", 6},
                {"H", 12},
                {"O", 6}
            }
    })

            Me.AddCompound(New Compound With {
        .Id = "pyruvate",
        .Name = "pyruvate",
        .Charge = -1,
        .StandardFormationEnergy = -472.0, ' Approximate
        .MolecularWeight = 87.05,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"C", 3},
                {"H", 3},
                {"O", 3}
            }
    })

            Me.AddCompound(New Compound With {
        .Id = "lactate",
        .Name = "L-lactate",
        .Charge = -1,
        .StandardFormationEnergy = -517.0, ' Approximate
        .MolecularWeight = 89.07,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"C", 3},
                {"H", 5},
                {"O", 3}
            }
    })

            Me.AddCompound(New Compound With {
        .Id = "NAD",
        .Name = "NAD+",
        .Charge = -1,
        .StandardFormationEnergy = -1059.0, ' Approximate
        .MolecularWeight = 663.43,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"C", 21},
                {"H", 27},
                {"N", 7},
                {"O", 14},
                {"P", 2}
            }
    })

            Me.AddCompound(New Compound With {
        .Id = "NADH",
        .Name = "NADH",
        .Charge = -2,
        .StandardFormationEnergy = -1012.0, ' Approximate
        .MolecularWeight = 665.44,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"C", 21},
                {"H", 29},
                {"N", 7},
                {"O", 14},
                {"P", 2}
            }
    })

            Me.AddCompound(New Compound With {
        .Id = "CO2",
        .Name = "carbon dioxide",
        .Charge = 0,
        .StandardFormationEnergy = -394.0,
        .MolecularWeight = 44.01,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"C", 1},
                {"O", 2}
            }
    })

            Me.AddCompound(New Compound With {
        .Id = "O2",
        .Name = "oxygen",
        .Charge = 0,
        .StandardFormationEnergy = 0.0,
        .MolecularWeight = 32.0,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"O", 2}
            }
    })
        End Sub
    End Class
End Namespace