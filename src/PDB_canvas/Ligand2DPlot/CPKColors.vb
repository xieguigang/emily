Imports Microsoft.VisualBasic.Serialization.JSON

''' <summary>
''' CPK着色方案（以Robert Corey, Linus Pauling, Walter Kolen三位科学家命名），这是分子模型中使用最广泛的颜色系统，用于区分不同元素。
''' </summary>
Public Class CPKColors

    Public Property name As String
    Public Property symbol As String
    Public Property color As String

    Shared ReadOnly resource As String =
        <JSON>
[
    {
      "name": "氢",
      "symbol": "H",
      "color": "#FFFFFF"
    },
    {
      "name": "碳",
      "symbol": "C",
      "color": "#909090"
    },
    {
      "name": "氮",
      "symbol": "N",
      "color": "#3050F8"
    },
    {
      "name": "氧",
      "symbol": "O",
      "color": "#FF0D0D"
    },
    {
      "name": "氟",
      "symbol": "F",
      "color": "#90E050"
    },
    {
      "name": "氯",
      "symbol": "Cl",
      "color": "#1FF01F"
    },
    {
      "name": "溴",
      "symbol": "Br",
      "color": "#A62929"
    },
    {
      "name": "碘",
      "symbol": "I",
      "color": "#940094"
    },
    {
      "name": "磷",
      "symbol": "P",
      "color": "#FF8000"
    },
    {
      "name": "硫",
      "symbol": "S",
      "color": "#FFFF30"
    },
    {
      "name": "钠",
      "symbol": "Na",
      "color": "#AB5CF2"
    },
    {
      "name": "钾",
      "symbol": "K",
      "color": "#AB5CF2"
    },
    {
      "name": "镁",
      "symbol": "Mg",
      "color": "#8AFF00"
    },
    {
      "name": "钙",
      "symbol": "Ca",
      "color": "#3DFF00"
    },
    {
      "name": "铁",
      "symbol": "Fe",
      "color": "#E06633"
    },
    {
      "name": "锌",
      "symbol": "Zn",
      "color": "#7D80B0"
    },
    {
      "name": "镍",
      "symbol": "Ni",
      "color": "#A52A2A"
    },
    {
      "name": "铜",
      "symbol": "Cu",
      "color": "#C88033"
    },
    {
      "name": "银",
      "symbol": "Ag",
      "color": "#C0C0C0"
    },
    {
      "name": "金",
      "symbol": "Au",
      "color": "#FFD700"
    },
    {
      "name": "铂",
      "symbol": "Pt",
      "color": "#E5E4E2"
    },
    {
      "name": "氦",
      "symbol": "He",
      "color": "#D9FFFF"
    },
    {
      "name": "氖",
      "symbol": "Ne",
      "color": "#B3E3F5"
    }
]</JSON>

    Public Shared Function LoadColors() As CPKColors()
        Return resource.LoadJSON(Of CPKColors())
    End Function

End Class
