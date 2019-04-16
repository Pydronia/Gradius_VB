Public Class GameWindow

	Private gm As GameManager

	' Window Constructor
	Public Sub New(ByVal gm As GameManager)
		InitializeComponent()
		Me.gm = gm
	End Sub

	' Setup game window
	Private Sub Window_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
		lblName.Text = gm.getName()
		lblLives.Text = gm.getLives()
	End Sub

	
End Class
