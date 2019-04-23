' Enum for different sounds
Public Enum SoundEffects
	Death
	GameOver
	Intro
	Volcano
	Shoot
End Enum

' Class to manage the playing of sound effects and background sounds.
Public Class SoundManager

	Private Delegate Sub soundEffectDelegate(ByVal effect As SoundEffects)

	Public backgroundPlayer As New MediaPlayer()
	Public bulletPlayer As New MediaPlayer()
	Public enemyPlayer As New MediaPlayer()

	Public currentMusic As SoundEffects

	Private gm As GameManager

	Public Sub New(ByVal gm As GameManager)
		Me.gm = gm
		currentMusic = SoundEffects.Intro
	End Sub

	Public Sub playSoundEffect(ByVal effect As SoundEffects)
		Select Case effect
			Case SoundEffects.Death, SoundEffects.GameOver, SoundEffects.Intro, SoundEffects.Volcano
				backgroundPlayer.Stop()
				playSound(backgroundPlayer, effect)
			Case SoundEffects.Shoot
				playSound(bulletPlayer, effect)
		End Select
	End Sub

	Private Sub playSound(ByRef player As MediaPlayer, ByVal effect As SoundEffects)
		player = New MediaPlayer()
		Dim uri As New Uri("Sounds/death.wav", UriKind.Relative)
		Select Case effect
			Case SoundEffects.Death
				uri = New Uri("Sounds/death.wav", UriKind.Relative)
			Case SoundEffects.GameOver
				uri = New Uri("Sounds/gameOver.mp3", UriKind.Relative)
			Case SoundEffects.Intro
				uri = New Uri("Sounds/intro.mp3", UriKind.Relative)
			Case SoundEffects.Volcano
				uri = New Uri("Sounds/volcano.mp3", UriKind.Relative)
			Case SoundEffects.Shoot
				uri = New Uri("Sounds/shoot.wav", UriKind.Relative)
		End Select
		player.Open(uri)

		If gm.getSoundSetting = False Then
			player.Volume = 0
		End If

		player.Play()
	End Sub

End Class

' Structure to represent directions
Public Structure Directions
	Public up As Boolean
	Public down As Boolean
	Public left As Boolean
	Public right As Boolean

	Public Shared Operator =(ByVal d1 As Directions, ByVal d2 As Directions) As Boolean
		Return (d1.up = d2.up And d1.down = d2.down And d1.left = d2.left And d1.right = d2.right)
	End Operator
	Public Shared Operator <>(ByVal d1 As Directions, ByVal d2 As Directions) As Boolean
		Return Not (d1.up = d2.up And d1.down = d2.down And d1.left = d2.left And d1.right = d2.right)
	End Operator
End Structure

' enum to represent what animation frame to display
Public Enum AnimationFrame
	Neutral
	Up
	Down
End Enum

' Class for storing the representation of maps and terrain
Public Class Map

	Const mapMoveSpeed As Integer = 64

	Public control As Image

	Public collisionMap As Boolean(,)

	Public length As Integer
	Public position As Double
	Public movementSpeed As Double

	Public Sub New(ByVal control As Image, ByVal collisionImage As BitmapImage)
		Me.control = control
		Me.length = control.Width
		Me.position = 0
		Me.movementSpeed = mapMoveSpeed

		Canvas.SetZIndex(Me.control, 2)

		' generating collision array
		' pixel data generation
		Dim stride As Integer = collisionImage.PixelWidth * collisionImage.Format.BitsPerPixel / 8
		Dim pixelArray((collisionImage.PixelHeight * stride) - 1) As Byte
		collisionImage.CopyPixels(pixelArray, stride, 0)

		collisionMap = New Boolean(collisionImage.PixelHeight - 1, collisionImage.PixelWidth - 1) {}

		' collisionmap generation (boolean array)
		Dim counter As Integer = 0
		For i = 0 To collisionImage.PixelHeight - 1
			For j = 0 To collisionImage.PixelWidth - 1
				If CInt(pixelArray(counter)) > 0 Then
					collisionMap(i, j) = False
				Else
					collisionMap(i, j) = True
				End If
				counter = counter + 4
			Next j
		Next i

	End Sub

	Public Sub ui_updateMapPosition()
		Canvas.SetLeft(control, position)
	End Sub

End Class

' Overall class for entities, including bullets, enemies, and the player
Public Class Entity

	Public movementDirection As Vector
	Public position As Point
	Public movementSpeed As Double
	Public hitBox As Rect
	Public freeDirections As Directions

	Public currentAnimationFrame As AnimationFrame
	Public animationFrames As BitmapImage()

	Public deathFrameNum As Integer
	Public deathAnimationFrames As BitmapImage()
	Public isDying As Boolean = False
	Public deathAnimationDelay As Integer
	Public deathAnimationDelayInterval As DateTime
	Public deathAnimationDelayStart As DateTime

	Public shallShoot As Boolean = False
	Public currentBullets(-1) As Entity

	Public shallDestroy As Boolean = False

	Public type As String
	Public control As Image

	Public Sub New(ByVal type As String, ByVal control As Image, ByVal position As Point, ByVal movementSpeed As Double)
		Me.type = type
		Me.control = control
		Me.position = position
		Me.movementSpeed = movementSpeed
		Me.deathAnimationDelay = 150
		Me.deathFrameNum = -1
		Me.hitBox = New Rect(position.X, position.Y, control.Width, control.Height)

		Canvas.SetZIndex(control, -1)

		currentAnimationFrame = AnimationFrame.Neutral

		freeDirections.up = True
		freeDirections.down = True
		freeDirections.left = True
		freeDirections.right = True
	End Sub

	' position update
	Public Sub ui_updatePosition()
		Canvas.SetLeft(control, position.X)
		Canvas.SetTop(control, position.Y)
		hitBox.X = position.X
		hitBox.Y = position.Y
	End Sub

	Public Sub ui_updateFrame()
		If Me.isDying Then
			If deathFrameNum >= deathAnimationFrames.Length Then
				control.Source = Nothing
			Else
				control.Source = deathAnimationFrames(deathFrameNum)
			End If
		Else
			control.Source = animationFrames(currentAnimationFrame)
		End If
	End Sub

	' update the position of the entity's bullets, and destroy them if needed.
	Public Sub ui_updateBullets(ByRef gw As GameWindow)
		Dim toDestroy(-1) As Integer

		For i = 0 To currentBullets.Length - 1
			If currentBullets(i).shallDestroy Then
				Array.Resize(toDestroy, toDestroy.Length + 1)
				toDestroy(toDestroy.Length - 1) = i
				gw.gameField.Children.Remove(currentBullets(i).control)
			End If
			currentBullets(i).ui_updatePosition()
		Next i

		For i = 0 To toDestroy.Length - 1
			Dim newArray(currentBullets.Length - 2) As Entity
			If newArray.Length <> 0 Then
				Array.Copy(currentBullets, 0, newArray, 0, toDestroy(i))
				Array.Copy(currentBullets, toDestroy(i) + 1, newArray, toDestroy(i), currentBullets.Length - 1 - toDestroy(i))
			End If
			currentBullets = newArray
		Next i
	End Sub

	Public Overridable Sub moveEntity(ByVal delta As TimeSpan)
		Dim distance = (delta.TotalMilliseconds / 1000) * movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(movementDirection * distance, position)
		position = newPosition

		If position.X > GameManager.gameWidth * GameManager.scaleFactor Or position.X < 0 Or position.Y > GameManager.gameHeight * GameManager.scaleFactor Or position.Y < 0 Then
			shallDestroy = True
		End If
	End Sub

	Public Overridable Sub generateBullet(ByRef gw As GameWindow, ByRef sm As SoundManager)

	End Sub

End Class

' The class for the player entity
Public Class VicViper
	Inherits Entity

	Public Const startingSpeed As Integer = 128
	Public Const startingPosX As Double = 50
	Public Const startingPosY As Double = 200
	Public Const vicResetDelay As Integer = 2000

	Private Const vicDeathDelayTime As Integer = 200


	Public directionKeys As Directions

	Public Sub New(ByVal control As Image)
		MyBase.New("vic", control, New Point(startingPosX, startingPosY), startingSpeed)

		Canvas.SetZIndex(Me.control, 0)

		animationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/vicViper.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Up.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Down.png")}
		deathAnimationFrames = New BitmapImage(3) {GameManager.makeNewBitmapImage("/Images/vicViper_Death_0.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Death_1.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Death_2.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Death_3.png")}
		Me.deathAnimationDelay = vicDeathDelayTime
	End Sub

	Public Sub resetState()
		isDying = False
		position = New Point(startingPosX, startingPosY)
		movementDirection = New Vector(0, 0)
		movementSpeed = startingSpeed
		currentAnimationFrame = AnimationFrame.Neutral
		deathFrameNum = -1
	End Sub

	Public Overrides Sub moveEntity(ByVal delta As TimeSpan)
		Dim distance = (delta.TotalMilliseconds / 1000) * movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(movementDirection * distance, position)

		' fix for possible boundary errors
		If newPosition.X < 0 Then
			newPosition.X = 0
		ElseIf newPosition.X > (GameManager.gameWidth * GameManager.scaleFactor) - 1 Then
			newPosition.X = (GameManager.gameWidth * GameManager.scaleFactor) - 1
		End If
		If newPosition.Y < 0 Then
			newPosition.Y = 0
		ElseIf newPosition.Y > (GameManager.gameHeight * GameManager.scaleFactor) - 1 Then
			newPosition.Y = (GameManager.gameHeight * GameManager.scaleFactor) - 1
		End If

		position = newPosition
	End Sub

	Public Overrides Sub generateBullet(ByRef gw As GameWindow, ByRef sm As SoundManager)
		If currentBullets.Length < 2 Then
			sm.playSoundEffect(SoundEffects.Shoot)
			Dim bullet As Entity
			bullet = New Entity("bullet", GameManager.makeNewSprite("/Images/vic_Bullet.png"), Point.Add(position, New Size(control.Width / 2, control.Height / 2)), 750)
			Array.Resize(currentBullets, currentBullets.Length + 1)
			currentBullets(currentBullets.Length - 1) = bullet
			bullet.ui_updatePosition()
			bullet.movementDirection = New Vector(1, 0)
			gw.gameField.Children.Add(bullet.control)
		End If
	End Sub

End Class


Public Class GameManager

	Public gw As GameWindow

	Private name As String
	Private soundSetting As Boolean
	Private highScore As Integer

#Region "GameSpecific"

	Public Shared scaleFactor As Integer = 2
	Public Shared gameWidth As Integer = 256
	Public Shared gameHeight As Integer = 208

	Const vicAnimationDelay As Integer = 100
	Const gameOverDelay As Integer = 2500

	Public vicViper As VicViper

	Public map As Map

	Private sm As SoundManager

	Private lives As Integer
	Private score As Integer

	Private gameTimer As Timers.Timer
	Private previousTime As DateTime
	Private deltaLoopTime As TimeSpan

	Private vicAnimationTicking As Boolean
	Private vicAnimationDelayStart As DateTime

	Private shallReset As Boolean = False
	Private shallEndGame As Boolean = False
	Private gameOver As Boolean = False
	Private gameOverDelayStart As DateTime


#End Region

#Region "Delegates"
	Private Delegate Sub InvokeDelegate()
#End Region

#Region "Constructor/Getters"

	' Instance Constructor
	Public Sub New(ByVal name As String, ByVal soundSetting As Boolean)
		Me.name = name
		Me.soundSetting = soundSetting
		preSetup()
	End Sub

	Public Function getName() As String
		Return name
	End Function

	Public Function getLives() As Integer
		Return lives
	End Function

	Public Function getScore() As Integer
		Return score
	End Function

	Public Function getHighScore() As Integer
		Return highScore
	End Function

	' dummy/stub function!
	Public Function getDummyScore() As Integer
		Return 1337008
	End Function

	Public Function getSoundSetting() As Boolean
		Return soundSetting
	End Function

#End Region

#Region "Input Handling"

	' Set the direction of the vic viper
	Public Sub setInputDirection(ByVal press As Boolean, ByVal key As Key)
		Select Case key
			Case Input.Key.Up
				vicViper.directionKeys.up = press
			Case Input.Key.Down
				vicViper.directionKeys.down = press
			Case Input.Key.Left
				vicViper.directionKeys.left = press
			Case Input.Key.Right
				vicViper.directionKeys.right = press
		End Select

		updateVicVector()
	End Sub

	' Signal to shoot!
	Public Sub prepareToShoot()
		vicViper.shallShoot = True
	End Sub

#End Region

#Region "Initialisation/Reset Routines"

	' setup before game window created
	Private Sub preSetup()
		lives = 3
		highScore = HighScoreManager.getHighScore()
		sm = New SoundManager(Me)
	End Sub

	' setup when game window created
	Public Sub setup()
		previousTime = DateTime.Now

		vicViper = New VicViper(makeNewSprite("/Images/vicViper.png"))
		vicViper.ui_updatePosition()
		gw.gameField.Children.Add(vicViper.control)

		map = New Map(makeNewSprite("/Images/map.png"), makeNewBitmapImage("/Images/collisionMap.png"))
		map.ui_updateMapPosition()
		gw.gameField.Children.Add(map.control)

	End Sub

	' start the game loop!
	Public Sub start()
		sm.playSoundEffect(SoundEffects.Intro)
		gameTimer = New Timers.Timer(1)
		AddHandler gameTimer.Elapsed, AddressOf gameLoop
		gameTimer.AutoReset = False
		gameTimer.Enabled = True
	End Sub

	' Reset for when you loose a life
	Private Sub reset()
		Canvas.SetZIndex(vicViper.control, 0)
		shallReset = False

		vicViper.resetState()

		map.position = 0
		sm.playSoundEffect(SoundEffects.Intro)
		sm.currentMusic = SoundEffects.Intro
	End Sub

	Private Sub endGame()
		sm.playSoundEffect(SoundEffects.GameOver)
		Dim sw As ScoreWindow
		sw = New ScoreWindow(Me)
		gw.Close()
		sw.Show()
	End Sub

#End Region

#Region "Main Game Loop"
	' The game loop which runs once every ~15-16ms. Due to the nature of timers it is not always exact, however it is inconsequential and managable.
	Private Sub gameLoop(send As Object, e As Timers.ElapsedEventArgs)
		' delta time calculation (for accurate movement)
		Dim currentTime As DateTime
		currentTime = DateTime.Now
		deltaLoopTime = currentTime - previousTime
		previousTime = currentTime

		' Processing
		If checkCollisions() Then
			gw.Dispatcher.BeginInvoke(New InvokeDelegate(AddressOf destroyVicViper))
		ElseIf vicViper.isDying = False Then
			moveEntity(vicViper, deltaLoopTime)
		End If
		moveEntities(deltaLoopTime)
		updateTimedRoutines()
		moveMap(deltaLoopTime)

		' UI Updating
		gw.Dispatcher.BeginInvoke(New InvokeDelegate(AddressOf updateUI))

		If shallEndGame = False Then
			gameTimer.Enabled = True
		End If
	End Sub
#End Region

#Region "Processing Routines"

	' Check the collisions for the vicviper/entities
	Private Function checkCollisions()

		' vic viper checking
		For i = 0 To vicViper.currentBullets.Length - 1
			If checkTerrain(vicViper.currentBullets(i)) Then
				vicViper.currentBullets(i).shallDestroy = True
			End If
		Next i

		Dim hasDied As Boolean
		hasDied = False
		If Not vicViper.isDying Then
			checkBoundaries()
			hasDied = checkTerrain(vicViper)
		End If
		Return hasDied
	End Function

	Private Sub checkBoundaries()
		Dim previous As Directions
		previous = vicViper.freeDirections

		vicViper.freeDirections.up = vicViper.hitBox.Top > 4
		vicViper.freeDirections.left = vicViper.hitBox.Left > 4
		vicViper.freeDirections.down = vicViper.hitBox.Bottom < (gameHeight * scaleFactor) - 5
		vicViper.freeDirections.right = vicViper.hitBox.Right < (gameWidth * scaleFactor) - 5

		If previous <> vicViper.freeDirections Then
			updateVicVector()
		End If
	End Sub

	Private Function checkTerrain(ByVal entity As Entity)
		Dim collided As Boolean
		collided = False

		Dim topLeftCoord As Point
		topLeftCoord = New Point(CInt(Int((entity.hitBox.X - map.position) / (scaleFactor * 2))), CInt(Int((entity.hitBox.Y) / (scaleFactor * 2))))
		Dim bottomRightCoord As Point
		bottomRightCoord = New Point(CInt(Int((entity.hitBox.Right - map.position) / (scaleFactor * 2))), CInt(Int((entity.hitBox.Bottom) / (scaleFactor * 2))))

		Dim i As Integer = topLeftCoord.Y
		Do Until collided Or i > bottomRightCoord.Y
			Dim j As Integer = topLeftCoord.X
			Do Until collided Or j > bottomRightCoord.X
				If map.collisionMap(i, j) Then
					collided = True
				End If
				j = j + 1
			Loop
			i = i + 1
		Loop

		Return collided
	End Function

	' move the specified entity and update vicViper animation frame if applicable
	Private Sub moveEntity(ByVal entity As Entity, ByVal delta As TimeSpan)
		entity.moveEntity(delta)
	End Sub

	Private Sub moveEntities(ByVal delta As TimeSpan)
		For i = 0 To vicViper.currentBullets.Length - 1
			vicViper.currentBullets(i).moveEntity(delta)
		Next i
	End Sub

	Private Sub moveMap(ByVal delta As TimeSpan)
		map.position = map.position - ((delta.TotalMilliseconds / 1000) * map.movementSpeed)

		If map.position <= (-map.length + (gameWidth * scaleFactor)) Then
			map.position = 0
		End If
	End Sub

	' update and check events which happen on a timed basis
	Private Sub updateTimedRoutines()
		If Not vicViper.isDying Then
			updateVicAnimationFrame()
		Else
			' play death animation
			If (DateTime.Now - vicViper.deathAnimationDelayInterval).TotalMilliseconds >= vicViper.deathAnimationDelay Then
				vicViper.deathAnimationDelayInterval = DateTime.Now
				vicViper.deathFrameNum = vicViper.deathFrameNum + 1
			End If

			' check if should reset
			If (DateTime.Now - vicViper.deathAnimationDelayStart).TotalMilliseconds >= vicViper.vicResetDelay And Not gameOver Then
				shallReset = True
			End If

			' check if should end game
			If gameOver AndAlso (DateTime.Now - gameOverDelayStart).TotalMilliseconds >= gameOverDelay Then
				shallEndGame = True
			End If
		End If
	End Sub

	' update the animation frame of the vic viper
	Private Sub updateVicAnimationFrame()
		If vicAnimationTicking And (DateTime.Now - vicAnimationDelayStart).TotalMilliseconds >= vicAnimationDelay Then
			vicAnimationTicking = False
			If vicViper.movementDirection.Y > 0 Then
				vicViper.currentAnimationFrame = AnimationFrame.Down
			ElseIf vicViper.movementDirection.Y < 0 Then
				vicViper.currentAnimationFrame = AnimationFrame.Up
			End If
		End If
	End Sub

	' Update the vicViper's movement vector and start animation frame delay
	Private Sub updateVicVector()
		Dim previous As Vector
		previous = vicViper.movementDirection

		If Not vicViper.isDying Then
			' calculate vector
			Dim newVector As Vector
			newVector = New Vector()
			If (vicViper.directionKeys.up And vicViper.directionKeys.down) Or Not (vicViper.directionKeys.up Or vicViper.directionKeys.down) Or (vicViper.directionKeys.up And Not vicViper.freeDirections.up) Or (vicViper.directionKeys.down And Not vicViper.freeDirections.down) Then
				newVector.Y = 0
			ElseIf vicViper.directionKeys.up Then
				newVector.Y = -1
			ElseIf vicViper.directionKeys.down Then
				newVector.Y = 1
			End If

			If (vicViper.directionKeys.left And vicViper.directionKeys.right) Or Not (vicViper.directionKeys.left Or vicViper.directionKeys.right) Or (vicViper.directionKeys.left And Not vicViper.freeDirections.left) Or (vicViper.directionKeys.right And Not vicViper.freeDirections.right) Then
				newVector.X = 0
			ElseIf vicViper.directionKeys.left Then
				newVector.X = -1
			ElseIf vicViper.directionKeys.right Then
				newVector.X = 1
			End If

			'If newVector.Length <> 0 Then
			'	newVector.Normalize()
			'End If

			vicViper.movementDirection = newVector
		End If

		' animation timer start
		If vicViper.movementDirection.Y = 0 Then
			vicAnimationTicking = False
			vicViper.currentAnimationFrame = AnimationFrame.Neutral
		ElseIf previous.Y <> vicViper.movementDirection.Y Then
			vicAnimationTicking = True
			vicAnimationDelayStart = DateTime.Now
		End If

	End Sub

	Private Sub destroyVicViper()
		vicViper.isDying = True
		lives = lives - 1

		If lives < 0 Then
			gameOver = True
			gameOverDelayStart = DateTime.Now
		End If

		Canvas.SetZIndex(vicViper.control, 50)
		vicViper.deathFrameNum = 0
		vicViper.deathAnimationDelayInterval = DateTime.Now
		vicViper.deathAnimationDelayStart = DateTime.Now

		sm.playSoundEffect(SoundEffects.Death)

	End Sub
#End Region

#Region "UI Update Routines"
	' updates all the UI elements
	Private Sub updateUI()
		If vicViper.shallShoot Then
			vicViper.generateBullet(gw, sm)
			vicViper.shallShoot = False
		End If

		If shallReset Then
			reset()
		ElseIf shallEndGame Then
			endGame()
		Else
			vicViper.ui_updatePosition()
			vicViper.ui_updateFrame()
			vicViper.ui_updateBullets(gw)
			map.ui_updateMapPosition()
			updateBGM()
			updateInterface()
		End If
	End Sub

	' updates text elements
	Private Sub updateInterface()
		If lives >= 0 Then
			gw.lblLives.Text = getLives()
		Else
			gw.lblLives.Visibility = Visibility.Hidden
		End If
	End Sub

	' updates which BGM to use
	Private Sub updateBGM()
		If sm.currentMusic = SoundEffects.Intro AndAlso map.position < -810 * scaleFactor Then
			sm.currentMusic = SoundEffects.Volcano
			sm.playSoundEffect(SoundEffects.Volcano)
		End If
	End Sub

#End Region

#Region "Other routines"

	' helper function to make a new sprite 
	Public Shared Function makeNewSprite(ByVal source As String) As Image
		Dim newControl As New Image()
		Dim newBitmap As BitmapImage
		newBitmap = makeNewBitmapImage(source)
		newControl.Source = newBitmap
		newControl.Width = newBitmap.PixelWidth * GameManager.scaleFactor
		newControl.Height = newBitmap.PixelHeight * GameManager.scaleFactor
		Return newControl
	End Function

	' helper function to make new image for sprite
	Public Shared Function makeNewBitmapImage(ByVal source As String) As BitmapImage
		Dim newBitmap As New BitmapImage(New Uri("pack://application:,,," & source))
		Return newBitmap
	End Function
#End Region


End Class
