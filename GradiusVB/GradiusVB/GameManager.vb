' Enum for different sounds
Public Enum SoundEffects
	Death
	GameOver
	Intro
	Volcano
	Shoot
	Kill
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
			Case SoundEffects.Kill
				playSound(enemyPlayer, effect)
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
			Case SoundEffects.Kill
				uri = New Uri("Sounds/kill.wav", UriKind.Relative)
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
	Three
	Four
	Five
End Enum

' Class for storing the representation of maps and terrain
Public Class Map

	Const mapMoveSpeed As Integer = 64

	Public control As Image

	Public collisionMap As Boolean(,)
	Public enemyMap As Entity(,)

	Public length As Integer
	Public position As Double
	Public movementSpeed As Double

	Public Sub New(ByVal control As Image, ByVal collisionImage As BitmapImage, ByVal enemyMapImage As BitmapImage)
		Me.control = control
		Me.length = control.Width
		Me.position = 0
		Me.movementSpeed = mapMoveSpeed

		Canvas.SetZIndex(Me.control, 2)

		' collisionmap generation
		Dim collisionPixelArray As Byte()
		collisionPixelArray = GameManager.getPixelData(collisionImage)

		collisionMap = New Boolean(collisionImage.PixelHeight - 1, collisionImage.PixelWidth - 1) {}

		Dim counter As Integer = 0
		For i = 0 To collisionImage.PixelHeight - 1
			For j = 0 To collisionImage.PixelWidth - 1
				If CInt(collisionPixelArray(counter)) > 0 Then
					collisionMap(i, j) = False
				Else
					collisionMap(i, j) = True
				End If
				counter = counter + 4
			Next j
		Next i

		' enemyMap generation
		enemyMap = GameManager.generateEnemyMap(GameManager.getPixelData(enemyMapImage), New Size(enemyMapImage.PixelWidth, enemyMapImage.PixelHeight))

	End Sub

	Public Sub ui_updateMapPosition()
		Canvas.SetLeft(control, position)
	End Sub

End Class

' Overall class for entities, including bullets, enemies, and the player
Public Class Entity

	Public movementDirection As Vector
	Public position As Point
	Public startingPosition As Point
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

	Public Const animationCycleTime As Integer = 200
	Public lastFrame As DateTime

	Public shallShoot As Boolean = False
	Public shallChirp As Boolean = False
	Public currentBullets(-1) As Entity

	Public shallDestroy As Boolean = False
	Public shallSpawn As Boolean = False

	Public value As Integer
	Public dropsPowerUp As Boolean = False

	Public type As String
	Public control As Image

	Public Sub New(ByVal type As String, ByVal control As Image, ByVal position As Point, ByVal movementSpeed As Double)
		Me.type = type
		Me.control = control
		Me.position = position
		Me.startingPosition = position
		Me.movementSpeed = movementSpeed
		Me.deathAnimationDelay = 64
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
	End Sub

	Public Sub ui_updateFrame()
		If Me.isDying Then
			If deathFrameNum >= deathAnimationFrames.Length Then
				control.Source = Nothing
			Else
				control.Source = deathAnimationFrames(deathFrameNum)
				control.Width = deathAnimationFrames(deathFrameNum).PixelWidth * GameManager.scaleFactor
				control.Height = deathAnimationFrames(deathFrameNum).PixelHeight * GameManager.scaleFactor
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

		GameManager.removeFromArray(currentBullets, toDestroy)

	End Sub

	Public Overridable Sub moveEntity(ByVal delta As TimeSpan, ByRef vicViper As VicViper)
		Dim distance = (delta.TotalMilliseconds / 1000) * movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(movementDirection * distance, position)
		position = newPosition
		hitBox.X = position.X
		hitBox.Y = position.Y

		If position.X > GameManager.gameWidth * GameManager.scaleFactor Or position.X < 0 Or position.Y > GameManager.gameHeight * GameManager.scaleFactor Or position.Y < 0 Then
			shallDestroy = True
		End If
	End Sub

	Public Overridable Sub generateBullet(ByRef gw As GameWindow, ByRef sm As SoundManager)

	End Sub

	Public Overridable Sub resetState()
		Me.position = startingPosition
		Me.shallDestroy = False
		Me.isDying = False
		Me.shallChirp = False
		Me.shallShoot = False
		Me.shallSpawn = False
		Me.deathFrameNum = -1
		currentAnimationFrame = AnimationFrame.Neutral
		ui_updatePosition()
		ui_updateFrame()
		hitBox.X = position.X
		hitBox.Y = position.Y
	End Sub

End Class

' class for enemy
Public Class Enemy_Fan
	Inherits Entity

	' behaviour variables
	Const turnPosition As Integer = 200
	Private aboveFan As Boolean = False
	Private hasTurned As Boolean = False
	Private hasStraightened As Boolean = False

	Sub New(ByVal position As Point)
		MyBase.New("fan", GameManager.makeNewSprite("/Images/fan_1.png"), position, 256)
		movementDirection = New Vector(-1, 0)

		animationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/fan_1.png"), GameManager.makeNewBitmapImage("/Images/fan_2.png"), GameManager.makeNewBitmapImage("/Images/fan_3.png")}
		deathAnimationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/fan_death_1.png"), GameManager.makeNewBitmapImage("/Images/fan_death_2.png"), GameManager.makeNewBitmapImage("/Images/fan_death_3.png")}

		value = 100

		lastFrame = DateTime.Now
	End Sub

	Public Overrides Sub moveEntity(ByVal delta As TimeSpan, ByRef vicViper As VicViper)
		If position.X <= turnPosition And hasTurned = False Then
			hasTurned = True
			If position.Y > GameManager.gameHeight * GameManager.scaleFactor / 2 Then
				movementDirection = New Vector(0.5, -1)
			Else
				aboveFan = True
				movementDirection = New Vector(0.5, 1)
			End If

		End If

		If hasTurned And Not hasStraightened Then
			If (aboveFan And position.Y > vicViper.position.Y) Or (Not aboveFan And position.Y < vicViper.position.Y) Then
				hasStraightened = True
				movementDirection = New Vector(1.5, 0)
			End If
		End If


		Dim distance = (delta.TotalMilliseconds / 1000) * movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(movementDirection * distance, position)
		position = newPosition
		hitBox.X = position.X
		hitBox.Y = position.Y

		If position.X > GameManager.gameWidth * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.X < -GameManager.enemyBufferZone Or position.Y > GameManager.gameHeight * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.Y < -GameManager.enemyBufferZone Then
			shallDestroy = True
		End If
	End Sub

	Public Overrides Sub resetState()
		MyBase.resetState()
		hasTurned = False
		hasStraightened = False
		movementDirection = New Vector(-1, 0)
	End Sub

End Class

' class for enemy
Public Class Enemy_Rugal
	Inherits Entity

	Sub New(ByVal position As Point)
		MyBase.New("rugal", GameManager.makeNewSprite("/Images/rugal_1.png"), position, 256)
		movementDirection = New Vector(-1, 0)

		animationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/rugal_1.png"), GameManager.makeNewBitmapImage("/Images/rugal_2.png"), GameManager.makeNewBitmapImage("/Images/rugal_3.png")}
		deathAnimationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/death_1.png"), GameManager.makeNewBitmapImage("/Images/death_2.png"), GameManager.makeNewBitmapImage("/Images/death_3.png")}

		value = 100
	End Sub

	Public Overrides Sub moveEntity(ByVal delta As TimeSpan, ByRef vicViper As VicViper)

		If position.Y > vicViper.position.Y + 2 Then
			movementDirection = New Vector(-0.5, -0.15)
			currentAnimationFrame = AnimationFrame.Up
		ElseIf position.Y < vicViper.position.Y - 2 Then
			movementDirection = New Vector(-0.5, 0.15)
			currentAnimationFrame = AnimationFrame.Down
		Else
			movementDirection = New Vector(-1, 0)
			currentAnimationFrame = AnimationFrame.Neutral
		End If


		Dim distance = (delta.TotalMilliseconds / 1000) * movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(movementDirection * distance, position)
		position = newPosition
		hitBox.X = position.X
		hitBox.Y = position.Y

		If position.X > GameManager.gameWidth * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.X < -GameManager.enemyBufferZone Or position.Y > GameManager.gameHeight * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.Y < -GameManager.enemyBufferZone Then
			shallDestroy = True
		End If
	End Sub

End Class

' class for enemy
Public Class Enemy_Garun
	Inherits Entity

	Private baseLinePos As Double

	Sub New(ByVal position As Point, ByVal isRed As Boolean)
		MyBase.New("garun", GameManager.makeNewSprite("/Images/garun_1.png"), position, 256)
		movementDirection = New Vector(-1, 0)

		baseLinePos = position.Y

		If isRed Then
			dropsPowerUp = True
			animationFrames = New BitmapImage(3) {GameManager.makeNewBitmapImage("/Images/garun_1_red.png"), GameManager.makeNewBitmapImage("/Images/garun_2_red.png"), GameManager.makeNewBitmapImage("/Images/garun_3_red.png"), GameManager.makeNewBitmapImage("/Images/garun_4_red.png")}
		Else
			animationFrames = New BitmapImage(3) {GameManager.makeNewBitmapImage("/Images/garun_1.png"), GameManager.makeNewBitmapImage("/Images/garun_2.png"), GameManager.makeNewBitmapImage("/Images/garun_3.png"), GameManager.makeNewBitmapImage("/Images/garun_4.png")}
		End If

		deathAnimationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/death_1.png"), GameManager.makeNewBitmapImage("/Images/death_2.png"), GameManager.makeNewBitmapImage("/Images/death_3.png")}

		value = 100

		lastFrame = DateTime.Now
	End Sub

	Public Overrides Sub moveEntity(ByVal delta As TimeSpan, ByRef vicViper As VicViper)


		' TODO here


		Dim distance = (delta.TotalMilliseconds / 1000) * movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(movementDirection * distance, position)
		position = newPosition

		position.Y = baseLinePos + 35 * Math.Sin(0.0185 * position.X)

		hitBox.X = position.X
		hitBox.Y = position.Y

		If position.X > GameManager.gameWidth * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.X < -GameManager.enemyBufferZone Or position.Y > GameManager.gameHeight * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.Y < -GameManager.enemyBufferZone Then
			shallDestroy = True
		End If
	End Sub

End Class

Public Class Enemy_Dee01
	Inherits Entity

	Sub New(ByVal position As Point, ByVal onRoof As Boolean)
		MyBase.New("dee01", GameManager.makeNewSprite("/Images/dee01_1.png"), position, 64)
		movementDirection = New Vector(-1, 0)

		If onRoof Then
			animationFrames = New BitmapImage(5) {GameManager.makeNewBitmapImage("/Images/dee01_1_r.png"), GameManager.makeNewBitmapImage("/Images/dee01_2_r.png"), GameManager.makeNewBitmapImage("/Images/dee01_3_r.png"), GameManager.makeNewBitmapImage("/Images/dee01_4_r.png"), GameManager.makeNewBitmapImage("/Images/dee01_5_r.png"), GameManager.makeNewBitmapImage("/Images/dee01_6_r.png")}
		Else
			animationFrames = New BitmapImage(5) {GameManager.makeNewBitmapImage("/Images/dee01_6.png"), GameManager.makeNewBitmapImage("/Images/dee01_5.png"), GameManager.makeNewBitmapImage("/Images/dee01_4.png"), GameManager.makeNewBitmapImage("/Images/dee01_3.png"), GameManager.makeNewBitmapImage("/Images/dee01_2.png"), GameManager.makeNewBitmapImage("/Images/dee01_1.png")}
		End If

		deathAnimationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/death_1.png"), GameManager.makeNewBitmapImage("/Images/death_2.png"), GameManager.makeNewBitmapImage("/Images/death_3.png")}

		value = 100
	End Sub

	Public Overrides Sub moveEntity(ByVal delta As TimeSpan, ByRef vicViper As VicViper)



		Dim distance = (delta.TotalMilliseconds / 1000) * movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(movementDirection * distance, position)
		position = newPosition
		hitBox.X = position.X
		hitBox.Y = position.Y

		If position.X > GameManager.gameWidth * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.X < -GameManager.enemyBufferZone Or position.Y > GameManager.gameHeight * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.Y < -GameManager.enemyBufferZone Then
			shallDestroy = True
		End If
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

	Public Overrides Sub resetState()
		isDying = False
		position = New Point(startingPosX, startingPosY)
		movementDirection = New Vector(0, 0)
		movementSpeed = startingSpeed
		currentAnimationFrame = AnimationFrame.Neutral
		deathFrameNum = -1
	End Sub

	Public Overrides Sub moveEntity(ByVal delta As TimeSpan, ByRef vicViper As VicViper)
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
		hitBox.X = position.X
		hitBox.Y = position.Y
	End Sub

	Public Overrides Sub generateBullet(ByRef gw As GameWindow, ByRef sm As SoundManager)
		If currentBullets.Length < 2 And Not isDying Then
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
	Public Shared enemyBufferZone As Integer = 64

	Const vicAnimationDelay As Integer = 100
	Const gameOverDelay As Integer = 2500


	Public vicViper As VicViper

	Public map As Map
	Private previousEnemyCheckPosition As Double

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

	Private currentEnemies(-1) As Entity


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
		score = 0
		highScore = HighScoreManager.getHighScore()
		sm = New SoundManager(Me)
	End Sub

	' setup when game window created
	Public Sub setup()
		previousTime = DateTime.Now

		vicViper = New VicViper(makeNewSprite("/Images/vicViper.png"))
		vicViper.ui_updatePosition()
		gw.gameField.Children.Add(vicViper.control)

		map = New Map(makeNewSprite("/Images/map.png"), makeNewBitmapImage("/Images/collisionMap.png"), makeNewBitmapImage("/Images/enemy_map.png"))
		map.ui_updateMapPosition()
		gw.gameField.Children.Add(map.control)

		previousEnemyCheckPosition = -map.position + (gameWidth * scaleFactor) + enemyBufferZone
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

		For i = 0 To currentEnemies.Length - 1
			currentEnemies(i).resetState()
			gw.gameField.Children.Remove(currentEnemies(i).control)
		Next i

		ReDim currentEnemies(-1)

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

		' vic viper bullets checking
		For i = 0 To vicViper.currentBullets.Length - 1
			If checkTerrain(vicViper.currentBullets(i)) Then
				vicViper.currentBullets(i).shallDestroy = True
			Else
				Dim enemy As Entity = checkHitsForBullet(vicViper.currentBullets(i))
				If enemy IsNot Nothing Then
					score = score + enemy.value
					vicViper.currentBullets(i).shallDestroy = True
					enemy.isDying = True
					enemy.shallChirp = True
				End If
			End If
		Next i

		' vicViper checking
		Dim hasDied As Boolean
		hasDied = False
		If Not vicViper.isDying Then
			checkBoundaries()
			hasDied = checkTerrain(vicViper) Or checkEnemyCollision()
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

	Private Function checkHitsForBullet(ByRef bullet As Entity) As Entity
		Dim hitEnemy As Entity
		hitEnemy = Nothing
		For i = 0 To currentEnemies.Length - 1
			If bullet.hitBox.IntersectsWith(currentEnemies(i).hitBox) And Not currentEnemies(i).isDying Then
				hitEnemy = currentEnemies(i)
			End If
		Next i
		Return hitEnemy
	End Function

	Private Function checkEnemyCollision() As Boolean
		Dim hit As Boolean = False
		For i = 0 To currentEnemies.Length - 1
			If currentEnemies(i).hitBox.IntersectsWith(vicViper.hitBox) And Not currentEnemies(i).isDying Then
				hit = True
			End If
		Next i
		Return hit
	End Function

	' move the specified entity and update vicViper animation frame if applicable
	Private Sub moveEntity(ByVal entity As Entity, ByVal delta As TimeSpan)
		entity.moveEntity(delta, vicViper)
	End Sub

	Private Sub moveEntities(ByVal delta As TimeSpan)

		For i = 0 To currentEnemies.Length - 1
			If Not currentEnemies(i).isDying Then
				currentEnemies(i).moveEntity(delta, vicViper)
			End If
		Next i

		For i = 0 To vicViper.currentBullets.Length - 1
			vicViper.currentBullets(i).moveEntity(delta, vicViper)
		Next i
	End Sub

	Private Sub moveMap(ByVal delta As TimeSpan)
		map.position = map.position - ((delta.TotalMilliseconds / 1000) * map.movementSpeed)

		If map.position <= (-map.length + (gameWidth * scaleFactor)) Then
			map.position = 0
		End If

		' enemy spawning
		Dim currentEnemyCheckPosition As Double
		currentEnemyCheckPosition = -map.position + (gameWidth * scaleFactor) + enemyBufferZone

		Dim startPos As Integer
		startPos = CInt(Int(previousEnemyCheckPosition / (2 * scaleFactor))) Mod map.enemyMap.GetLength(1)
		Dim endPos As Integer
		endPos = CInt(Int(currentEnemyCheckPosition / (2 * scaleFactor))) Mod map.enemyMap.GetLength(1)

		If endPos - startPos > 0 Then
			' check columns for startPos + 1 -> endPos inclusive
			For i = startPos + 1 To endPos
				For j = 0 To map.enemyMap.GetLength(0) - 1
					If map.enemyMap(j, i) IsNot Nothing Then
						prepareEnemy(map.enemyMap(j, i))
					End If
				Next j
			Next i

		End If

		previousEnemyCheckPosition = currentEnemyCheckPosition

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

		For i = 0 To currentEnemies.Length - 1
			If currentEnemies(i).type = "fan" Or currentEnemies(i).type = "garun" Then

				If Not currentEnemies(i).isDying Then
					' regular animation
					If (DateTime.Now - currentEnemies(i).lastFrame).TotalMilliseconds >= Enemy_Fan.animationCycleTime Then
						currentEnemies(i).currentAnimationFrame = (CInt(currentEnemies(i).currentAnimationFrame) + 1) Mod currentEnemies(i).animationFrames.Length
						currentEnemies(i).lastFrame = DateTime.Now
					End If
				End If
			End If

			If currentEnemies(i).isDying Then
				If (DateTime.Now - currentEnemies(i).deathAnimationDelayInterval).TotalMilliseconds >= currentEnemies(i).deathAnimationDelay Then
					If currentEnemies(i).deathFrameNum >= currentEnemies(i).deathAnimationFrames.Length Then
						currentEnemies(i).shallDestroy = True
					End If
					currentEnemies(i).deathAnimationDelayInterval = DateTime.Now
					currentEnemies(i).deathFrameNum = currentEnemies(i).deathFrameNum + 1
				End If
			End If
		Next i

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

		' enemies
		Dim enemiesToDestroy(-1) As Integer
		For i = 0 To currentEnemies.Length - 1
			If currentEnemies(i).shallSpawn Then
				gw.gameField.Children.Add(currentEnemies(i).control)
				currentEnemies(i).shallSpawn = False
			ElseIf currentEnemies(i).shallDestroy Then
				currentEnemies(i).resetState()
				Array.Resize(enemiesToDestroy, enemiesToDestroy.Length + 1)
				enemiesToDestroy(enemiesToDestroy.Length - 1) = i
				gw.gameField.Children.Remove(currentEnemies(i).control)
			ElseIf currentEnemies(i).shallChirp Then
				sm.playSoundEffect(SoundEffects.Kill)
				currentEnemies(i).shallChirp = False
			End If
			currentEnemies(i).ui_updatePosition()
			currentEnemies(i).ui_updateFrame()
		Next i

		removeFromArray(currentEnemies, enemiesToDestroy)

		' vic Viper
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
		gw.lblScore.Text = getScore().ToString("D7")

	End Sub

	' updates which BGM to use
	Private Sub updateBGM()
		If sm.currentMusic = SoundEffects.Intro AndAlso Not vicViper.isDying AndAlso map.position < -810 * scaleFactor Then
			sm.currentMusic = SoundEffects.Volcano
			sm.playSoundEffect(SoundEffects.Volcano)
		End If
	End Sub

#End Region

#Region "Other routines"

	Private Sub prepareEnemy(ByVal enemy As Entity)
		Array.Resize(currentEnemies, currentEnemies.Length + 1)
		enemy.shallSpawn = True
		currentEnemies(currentEnemies.Length - 1) = enemy
	End Sub

	Public Shared Sub removeFromArray(ByRef array As Array, ByVal indexesToRemove As Integer())
		System.Array.Reverse(indexesToRemove)
		For i = 0 To indexesToRemove.Length - 1
			Dim newArray(array.Length - 2) As Entity
			If newArray.Length <> 0 Then
				array.Copy(array, 0, newArray, 0, indexesToRemove(i))
				array.Copy(array, indexesToRemove(i) + 1, newArray, indexesToRemove(i), array.Length - 1 - indexesToRemove(i))
			End If
			array = newArray
		Next i
	End Sub

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

	' helper to generate byte array from bitmap
	Public Shared Function getPixelData(ByVal collisionImage As BitmapImage) As Byte()
		Dim stride As Integer = collisionImage.PixelWidth * collisionImage.Format.BitsPerPixel / 8
		Dim pixelArray((collisionImage.PixelHeight * stride) - 1) As Byte
		collisionImage.CopyPixels(pixelArray, stride, 0)
		Return pixelArray
	End Function

	'' helper to generate map of enemies
	Public Shared Function generateEnemyMap(ByVal pixelArray As Byte(), ByVal dimensions As Size) As Entity(,)
		Dim enemyMap(dimensions.Height - 1, dimensions.Width - 1) As Entity

		Dim row As Integer = 0
		Dim column As Integer = 0

		For i = 0 To pixelArray.Length - 1 Step 4
			Dim colour As Color
			colour = Color.FromRgb(pixelArray(i + 2), pixelArray(i + 1), pixelArray(i))
			Dim toAdd As Entity
			toAdd = Nothing

			Select Case colour.ToString()
				Case "#FFFF0000"
					toAdd = New Enemy_Fan(New Point((gameWidth * scaleFactor) + enemyBufferZone, row * 2 * scaleFactor))
				Case "#FF00FF00"
					toAdd = New Enemy_Rugal(New Point((gameWidth * scaleFactor) + enemyBufferZone, row * 2 * scaleFactor))
				Case "#FF0000FF"
					toAdd = New Enemy_Garun(New Point((gameWidth * scaleFactor) + enemyBufferZone, row * 2 * scaleFactor), False)
				Case "#FF00F0FF"
					toAdd = New Enemy_Garun(New Point((gameWidth * scaleFactor) + enemyBufferZone, row * 2 * scaleFactor), True)
				Case "#FFFFFF00"
					toAdd = New Enemy_Dee01(New Point((gameWidth * scaleFactor) + enemyBufferZone, row * 2 * scaleFactor), False)
				Case "#FFFF00FF"
					toAdd = New Enemy_Dee01(New Point((gameWidth * scaleFactor) + enemyBufferZone, row * 2 * scaleFactor), True)
			End Select

			enemyMap(row, column) = toAdd

			column = column + 1
			If column = dimensions.Width Then
				column = 0
				row = row + 1
			End If
		Next i

		Return enemyMap
	End Function

#End Region


End Class
