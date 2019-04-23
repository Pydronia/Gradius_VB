Public Class GameWindow

	Private gm As GameManager

	' Window Constructor
	Public Sub New(ByVal gm As GameManager)
		InitializeComponent()
		Me.gm = gm
	End Sub

	' Setup game window
	Private Sub Window_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
		gm.gw = Me
		lblName.Text = gm.getName()
		lblLives.Text = gm.getLives()
		lblHi.Text = gm.getHighScore().ToString("D7")
		gm.setup()
		gm.start()
	End Sub

#Region "KeyInputHandlers"

	Private Sub Window_KeyDown(sender As System.Object, e As System.Windows.Input.KeyEventArgs) Handles MyBase.KeyDown
		If Not e.IsRepeat Then
			If (e.Key = Key.Up Or e.Key = Key.Down Or e.Key = Key.Left Or e.Key = Key.Right) Then
				gm.setInputDirection(True, e.Key)
			ElseIf e.Key = Key.A Then
				gm.prepareToShoot()
			End If
		End If
	End Sub

	Private Sub Window_KeyUp(sender As System.Object, e As System.Windows.Input.KeyEventArgs) Handles MyBase.KeyUp
		If Not e.IsRepeat And (e.Key = Key.Up Or e.Key = Key.Down Or e.Key = Key.Left Or e.Key = Key.Right) Then
			gm.setInputDirection(False, e.Key)
		End If
	End Sub

#End Region

	' Test end game
	Private Sub btnTst_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnTst.Click
		Dim sw As ScoreWindow
		sw = New ScoreWindow(gm)
		sw.Show()
		Me.Close()
	End Sub

	
End Class
