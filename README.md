# SearchLight


## 1. 구조 
<img width="960" alt="제목 없음" src="https://user-images.githubusercontent.com/37167860/192972452-84fd3f39-6c38-4024-94c8-53f49d53a4b4.png">

### 1) BackEnd (using BACKEND SDK)
#### (1) ServerManager
서버와 클라이언트간 로그인 접속 및 회원가입을 위한 서버 **API**로 InputField에 입력 된 ID/PW를 통해 서버에게 로그인 / 회원가입 요청을 진행한다. 

#### 실행 주기
> 어플리케이션 시작과 동시에 실행되며, 어플리케이션이 종료되기 전까지 종료 되지 않는다.  


#### 동작방식
> Login 버튼 클릭시 사용자의 ID와 PW가 서버에 전달되어 로그인 요청이 이루어지고, 로그인이 활성화 되었을 경우 **토큰**을 반환하여 어플리케이션이 종료되더라도 일정 시간 동안 재로그인 없이 로그인 활성화가 가능하다. 회원 가입의 경우도 Sign Up 버튼 클릭 시 사용자의 ID와 PW 그리고 닉네임이 서버에 전달되어 회원 가입 요청이 이루어지며 자동으로 로그인 요청이 이루어진다. <br/>

#### (2) ServerMatchManager
#### Partial – ServerMatchManager
 매칭 서버와 연결과 매칭 요청을 위한 매니저, MatchMakingHandler에 의해 매칭 서버와 연결을 진행하고 서버에 접속해 있는 사용자들에게 매칭 요청을 진행한다. 만약, 오류가 발생할 경우 Exception Handler에 의해 Error Message 표출을 각 Scene의 UIManager에게 요청한다.<br/>
<br>
#### Partial - ServerInGameManager
 인게임 서버와의 연결과 게임에서의 메시지 송수신을 위한 매니저, 같은 인게임 서버에 접속된 사용자가 송신하는 메시지를 BackEnd.Match.OnMatchRelay로 수신받아 클라이언트의 InGameManager에서 처리하도록 한다. 
<br>
#### 실행주기
> ServerManager와 동일하게 어플리케이션 시작과 동시에 실행되며 어플리케이션 종료 전까지 종료되지 않으며, Exception Handler를 제외하고 MatchScene에서 동작된다.

#### 동작방식    

>Login 버튼클릭 이후에 본격적으로 실행된다. Matching 버튼 클릭 시 사용자는 매칭 서버와 접속을 요청하게 된다. 접속이 정상 처리 될 경우, 매칭 서버는 만들어진 매칭 룸끼리 매칭을 이루어준다. 매칭이 정상 처리될 경우 매칭서버와의 연결을 종료하며, Ingame Scene으로 전환되고 유저들이 보내는 메세지를 받아 처리한다. 만약, 게임이 종료되어 종료 요청이 들어오면 게임 결과 창을 UI에 나타내며 동시에 인게임 서버의 접속을 종료 한다.

### 2) In Application
#### (1) GameManager
 현재 진행중인 게임의 상태를 저장하고, 핸들러를 통해 Scene을 전환하고 게임의 정보를 변경하는 매니저.<br><br>
#### 실행주기
> ServerManager와 동일하게 어플리케이션 시작과 동시에 실행되며, 어플리케이션이 종료되기 전까지 종료되지 않는다.

#### 동작방식
> 시작과 동시에 각 Scene에서 사용될 EventHandler들을 초기화하며, Scene전환 함수가 호출 되면 Scene을 전환하고 해당 Scene에 필요한 GameManager의 State 정보와 사용될 EventHandler들을 변경한다.

#### (2) Protocol
유저간 서버를 통해 주고 받는 메세지의 정보를 정의한다.

#### 동작방식
 주고 받는 인게임 메세지의 타입 다음과 같다.
> + **KeyMessage**
> 
>   사용자가 상호작용 버튼을 누를 시 전송되는 메시지로 가공되지 않은 메시지를 의미한다. 해당 메시지는 **HOST**에게 처리 되며, **HOST**는 Key값을 근거로 화면을 최신화하고 아래 각 메시지로 재가공하여 Broadcast 처리한다.<br/>
>
> + **AttackMessage**
> 
>   사용자가 공격 버튼을 누를 시 전송되는 메세지로, 공격하는 사용자의 SessionID, 방향 정보를 담고 있다. 해당 메시지를 송신 받으면 해당 사용자의 캐릭터에게 공격 애니메이션과 함수를 실행시킨다.
>
> + **StopAttackMessage**
> 
>   사용자가 공격 버튼을 땔 때 전송되는 메시지로, 공격하는 사용자의 SessionID를 담고 있다. 해당 메시지를 송신 받으면 해당 사용자의 캐릭터에게 공격 종료 애니메이션과 함수를 실행시킨다.
> 
> + **SwitchMessage**
> 
>   사용자가 총 변경 버튼을 누를 때 전송되는 메시지로, 사용자의 SessionID를 담고 있다. 해당 메시지를 송신 받으면 해당 사용자의 캐릭터에게 총 변경 애니메이션과 함수를 실행시킨다.
>
> + **RealoadMessage**
> 
>   사용자가 재장전 버튼을 누를 때 전송되는 메시지로, 사용자의 SessionID를 담고 있다. 해당 메시지를 송신 받으면 해당 사용자의 캐릭터에게 재장전 애니메이션과 함수를 실행시킨다.
> 
> + **AcquireMessage**
> 
>   사용자가 아이템을 습득할 때 전송되는 메시지로, 사용자의 SessionID와 동기화할 Position 값을 가지고 있다. 해당 메시지를 송신 받으면 해당 사용자의 캐릭터에게 습득 함수를 실행시킨다.
> 
> + **DamageMessage**
> 
>   사용자가 공격을 받을 때 전송되는 메시지로, 공격받는 사용자의 SessionID, 위치 정보를 담고 있다. 해당 메시지를 송신 받으면 해당 사용자의 캐릭터에게 피해 함수를 실행시킨다.
>  
> + **NoMoveMessage**
> 
>   사용자가 움직이지 않을 때, 사용자의 위치 정보가 변경 되지 않을 경우 전송 되는 메세지로, 사용자의 SessionID, 위치 정보를 담고 있다. 해당 메시지를 송신 받으면 해당 사용자의 캐릭터에게 이동 멈춤 애니메이션과 이동 멈춤 함수를 실행시킨다.
>  
> * **MoveMessage**
> 
>   유저가 움직일 때, 사용자의 위치 정보가 변경 되었을 경우 전송되는 메세지로, 유저의 SessionID, 위치, 방향 정보를 담고 있다. 해당 메시지를 송신 받으면 해당 사용자의 캐릭터에게 이동 애니메이션과 이동 함수를 실행시킨다.
>
> * **StartCountMessage**
> 
>   게임이 시작되기 전, 서버에 접속된 모든 클라이언트의 동기화를 위한 메세지로, 숫자 카운트 정보를 담고 있다. 해당 메시지를 송신 받으면 해당 사용자의 화면에 카운트 정보를 표시한다.
> 
> * **GameEndMessage**
> 
>   게임이 종료될 때, 서버에 접속된 모든 클라이언트의 동기화를 위한 메시지로, 게임 결과 창에 표시해야 하는 사용자의 정보들을 담고 있다. 해당 메시지를 송신 받으면 해당 사용자의 화면에 게임 결과 창을 출력한다.

#### (3) DataParser
서버에 보내거나 받은 메세지를 읽기 위해 위해 메세지를 인코딩/ 디코딩 작업을 한다. 
> ( 서버 - 클라이언트 : string[] -> byte[] // 클라이언트 - 서버 byte[] -> string[] ) 

### 3) In Game 
#### MANAGER
#### (1) WorldManager
 사용자의 업데이트 정보를 API를 통해 서버로 송신하거나, 수신된 메세지를 통해 인게임 정보를 수시로 업데이트하는 매니저.

#### 실행 주기
> Loading Scene에서 InGame Scene으로 전환되면서 실행되고, 인게임이 종료됨과 동시에 종료 된다.

#### 동작 방식
> 생성과 동시에 Initalization()을 통해 플레이어 정보(케릭터, 위치, 닉네임), StartCount와 같은 정보를 초기화하며. GameManager의 InGame.EventHandler를 최신화한다.
> 게임이 진행되면, MatchManager에 의해 정해진 HOST는 타 클라이언트가 불러오는 메세지를 OnReceive()를 통해 처리하지 않고 LocalQue에 담아놓고 OnReceiveForLocal()를 통해서 처리한다. LocalQueue에 담겨 있는 타 사용자의 KeyMessage는 Key Type에 따라 ProcessPlayerData()를 통해 처리하고 타 클라이언트들에게 재 송신한다.
> HOST가 아닌 클라이언트는 HOST에게 재가공 되는 메시지들을 OnReceive()를 통해 송신받아 ProcessPlayerData()를 통해 화면을 최신화한다.

#### (2) InGameUIManager
 게임에 남은 유저 수 및 카운트 다운을 표시하고 서버와 연결이 끊긴 사용자가 있을 경우 Reconnection UI를 표시한다. 캔버스에 존재하는 HUD UI오브젝트들과 PlayerHUD 스크립트를 연결시켜주는 역할을 수행하며 HPBar가 스크린 좌표계에서 Player의 3D 좌표를 추적할 수 있도록 연결시켜주는 초기화 작업을 수행한다.
 
#### 실행 주기
> InGameManager와 동일하게 InGame Scene으로 전환 되면서 실행되고, 인게임이 종료됨과 동시에 종료된다.

#### 동작 방식
> InGameManager에 의해 호출되며, 게임의 시작과 동시에 SetStartCount()로 게임의 시작을 알린다. 게임 종료 메시지를 수신할 경우 SetGameResult()를 통해 게임의 결과 창을 표시하고 Match Scene으로 화면을 전환한다.

#### (3) InputManager
 사용자의 조이스틱 정보를 서버에 전송시키기 위한 매니저이다.

#### 실행 주기
> InGameManager와 동일하게 InGame Scene으로 전환 되면서 실행되고, 인게임이 종료됨과 동시에 종료된다.

#### 동작 방식
> GameManager의 Ingame()이벤트를 호출하는 GameManager의 InGameUpdate 코루틴에 의해 매 프레임 호출된다. HOST일 경우 인풋 입력이 있는 순간마다 LocalQue에 메시지를 저장하고, 비 HOST일 경우 서버에게 SendDataToInGame()를 통해 KeyMessage를 송신한다.
> + 조이스틱 조작 (MoveInput()) : <br>
>   GameManager의 InGame액션에 바인드 되어 지속적으로 호출며어 Joystick X축과 Y축 값을 통해 입력 발생시 move 메시지를 발신한다.
> + Fire 버튼 바인드 (AddFireAction()) : <br>
>   Fire버튼이 ButtonUp 될때와 ButtonDown 될때 각각 호출되는 콜백 함수를 인자로 전달받아 바인드한다.
>
> + Reload 버튼 바인드 (AddReloadAciton()) : <br>
>   Reload 버튼 입력시 호출되는 콜백 함수를 인자로 전달받아 바인드한다.
>
> + WeaponChange 버튼 바인드 (AddWeaponChangeAction()) : <br>
>   WeaponChange 버튼 입력시 호출되는 콜백 함수를 인자로 전달받아 바인드한다.


#### (4) PoolingManager
 해당 매니저의 인스턴스 호출자로부터 pool id를 전달받아 해당 id에 해당하는 ObjectPool을 반환해주거나 새로 생성 혹은 삭제를 수행한다.
> + ObjectPool
>   Generic 클래스로 구현되어 다양한 타입에 대응할 수 있는 ObjectPool로, 사용자는PoolingManager를 통해  pool id를 부여받아 해당 id에 해당하는 ObjectPool을 사용할 수 있다.
>
> + PoolingParticle
>   ObjectPooling이 필요한 파티클에 부착되는 컴포넌트로, 해당 파티클의 재생이 종료되었을 때 ObjectPool에 return되는 기능을 수행한다.

#### 실행 주기
> InGameManager와 동일하게 InGame Scene으로 전환 되면서 실행되고, 인게임이 종료됨과 동시에 종료된다.

#### 동작 방식
> + 새로운 ObjectPool 생성을 위한ID 반환 (GetPoolID()) : <br>
> 서로 다른 오브젝트의 같은 스크립트에서 poolID를 요청할 경우 ID의 중복을 막기 위한 대책으로써 idMap을 활용하여 인자로 요청된 id가 idMap에 존재 하는지 여부를 분기로 동작한다.
>   1.	요청된 id가 idMap에 존재하지 않을 경우idMap에 id : 1을 등록한 후 ( key : val, 여기서 1은 다음에 같은 id로 요청이 들어올 시에 할당할 번호를 의미함) id + “0”을 반환한다.
>   2.	요청된 id가 idMap에 존재할 경우에는 해당 id에 해당하는 value가 현재 할당해줘야 할 번호를 의미하므로 id + idMap[id]를 반환하고 idMap++을 수행한다.	
> + 새로운 ObjectPool 생성 (AddObjectPool()): <br>
>   인자로 요청된 id가 poolMap에 존재하지 않을 경우에만 동작하며 poolMap에 대해 요청된 id를 key값으로, 새로운 ObjectPool을 그 value로 등록하면서 인자로 전달받은 Func, Action 구현부를 생성자로 전달한다.
> + ObjectPool 반환 (GetObjectPool()): <br>
>   이미 poolMap존재하는 id에 대해서만 동작하며 id에 매치되는 ObjectPool를 반환한다.


#### (5) SoundManager
  Reload, Fire, ItemGet, Dead, Hit등의 인게임 효과음에 대해 해당 매니저 인스턴스를 통해 원하는 위치에서 효과음을 재생할 수 있도록 PlayerSoundAtPoint 함수를 제공한다.
#### 실행 주기
> InGameManager와 동일하게 InGame Scene으로 전환 되면서 실행되고, 인게임이 종료됨과 동시에 종료된다.
#### 동작 방식
> 사운드 이펙트 재생 (PlaySoundAtPoint()): SoundEffect 열거형의 종류로 Reload, Fire1, Fire2, ItemGet, Dead, Hit 등이 존재하며 효과음 재생을 요구하는 스크립트에서 해당 함수의 인자로 원하는 타입을 전달하면 AudioSource.PlayerClipAtPoint(soundType, position)을 호출하여 특정된 위치에서 해당 효과음을 재생한다.

<br>
#### INTERFACE
#### (1) IDamageable
> 플레이어 외의 오브젝트에 데미지를 가하는 상황을 상정하여 피격 처리를 인터페이스화 시켰으며, TakeHit, TakeDamage, PlayerHitParticle 등의 메서드를 제공하고 해당 인터페이스를 상속받은 오브젝트는 Damageable Object로써 취급된다.

#### OBJECT
![KakaoTalk_20220926_235800900](https://user-images.githubusercontent.com/37167860/192311875-32f99b66-39ab-403c-a9d6-f2da643c1b23.png)
#### (1) Player
 hp(체력), shield(방어구), moveSpeed(이동속도), sightDistance(시야 거리), sightAngle(시야각)등의 필드를 관리하며 해당 정보가 요구되는 PlayerController, GunController, Detect 스크립트를 초기화 하는 역할을 수행하고 IDamageable로부터 실체화한 피격 처리 기능을 수행하며 실시간으로 MainCamera 위치를 갱신한다.
>
> + PlayerController <br>
>   플레이어의 이동, 회전 등의 Transform 관련 기능을 수행한다.
>
> + Detect <br>
>   플레이어의 시야 기능을 수행하며 SphereCollider를 통해 OnTriggerStay 되고 있는 특정 거리 내의 플레이어를 탐색하여 해당 플레이어의 정보를 각각의 플레이어 오브젝트 이름으로 구분하기 위해 detectedPlayers 딕셔너리에 추가하고 해당 플레이어가 OnTriggerExit 될 경우 딕셔너리에서 제외하며, detectedPlayers에 존재하는 플레이어가 자신의 시야각 내에 위치할 경우 해당 플레이어 오브젝트의 메시 렌더러를 활성화시키고 딕셔너리에서 제외되거나 시야각에서 벗어났을 경우 메시 렌더러를 비활성화시킨다.
>
> + PlayerHUD <br>
>   Player 스크립트의 각종 상태 변화 (OnHPChange, OnShieldChange, OnPlayerKillCountUpdate, OnAlivePlayerCountUpdate) 액션에 대해 각각의 업데이트 함수를 바인드하여 HUD 텍스트 및 이미지의 업데이트를 수행한다.
>
> + HealthBar <br>
>   Player의 체력 및 쉴드량 변화 액션(OnHPChange, OnShieldChange)에 대해 HP bar 및 Shield bar 갱신 함수를 바인드하여 HP bar를 관리한다.

#### (2) Gun
 GunController로부터 하달된 Fire 명령을 수행하며, Fire 함수 내부에서는 PoolingManager를 통해 할당받은 ObjectPool의 GetObject 함수에 의한 Projectile 오브젝트의 ObjectPooling이 수행된다.
>
> + GunController <br>
>   shotDistance (발사 거리), maxRecoilRadius (총기 반동 범위 상한), mainAmmoInPouch (보유 주무기 탄약), subAmmoInPouch(보유 보조무기 탄약) 등의 필드를 관리하며 보유중인 주무기를 mainGun으로, 보조무기를 subGun으로 관리하여 이를 통해 새로운 Gun 장비, Gun 교체, Reload, Fire 등의 기능을 수행한다.
>
> + Projectile <br>
>   Gun에서 발사되는 투사체로써의 역할을 수행하며 Raycast를 통한 충돌처리를 통해 대상이 Damageable Object인 경우에만 피격 이벤트를 발생시킨다.


#### (3) Item
HealPack, ShieldPack, MainAmmo, SubAmmo, MainWeapon(레벨0~3), SubWeapon(레벨1~3) 중에하나를 해당 아이템의 Category 로써 가질 수 있으며 아이템 획득자에게 현재 아이템 Category 에 따른 효과 혹은 새로운 무기를 부여한다.
>
> + ItemSpawn
>   Item을 스폰시키는 기능을 수행하며, 아이템 리스폰 쿨타임 (spawnCooltime), 스폰 시킬 아이템 (spawnType) 등의 필드를 가지고, 특정 아이템 고정 스폰 혹은 랜덤 스폰을 수행한다.


## 2. 동작
## Search Light


### 1) Login Scene
> ServerManager와 GameManager, 그리고 로그인에 사용될 UI를 관리할 LoginUIManager를 인스턴스화 하고, 유저의 접속을 기다린다. 유저가 로그인 또는 회원가입을 통해 서버접속을 원할 때 SeverManager는 해당 접근을 확인하고 승인 및 거절한다. ServerManager를 통해 Server의 승인이 되었을 경우 해당 유저에게 Login 토큰을 발행하여 일정 시간 자유롭게 로그인할 수 있는 권한을 주며, GameManager는 다음 Lobby Scene으로 전환한다.

### 2) Match Scene(Lobby)
> 로그인이 성공적으로 마무리될 때 Match Scene으로 전환되며, MatchManager의 동작 시작을 알리는 Initilalize()와 ServerMatchManager를 인스턴스화 하고, 사용자의 명령을 기다린다. Matching 버튼 클릭시 유저는 매칭 룸을 자동으로 생성하게 되며, 동시에 유저는 매칭 서버와 접속을 요청하게 된다. 접속이 정상 처리될 경우, 서버는 매칭 룸의 토큰을 발급하고, 자동으로 매칭서버에게 매칭을 요청한다. 매칭 서버는 만들어진 매칭 룸끼리 매칭룸 토큰을 사용해 매칭한다. 만약, 활성화 되어있는 룸 토큰이 없을 경우 샌드박스(인공지능) 모드로 진행된다. 매칭이 정상 처리 될 경우, Loading Scene으로 전환되며, ServerInGameMeanager에 의해 InGame Server와의 접속을 요청한다.

### 3) Loading Scene
> InGame Server와의 정상 접속 처리 후, 서버내에 접속 된 유저들의 정보를 SessionID을 통해 유저 카드를 생성하고 UIManager에 통해 화면에 표시한다. Scene이 유지된 채, InGame Scene의 설정이 완료될 때까지 기다린다.

### 4) InGame Scene
> InGameManager와 InputManager를 생성하고, InGame Scene에서 사용될 UI를을 처리하는 InGameUIManager를 인스턴스화 한다. 인게임 내에서 조이스틱의 인풋이 있다면, GameManager.InGameUpdate() 코루틴에 의해 InGame() EventHandler가 실행되고 MatchManager를 통해 메시지를 송신한다. KeyMessage를 수신 받은 클라이언트가 HOST라면, InGameManager.OnReceiveForLocal()을 통해 처리하며, MoveMessage, AttackMessage, DamageMessage, NoMoveMessage와 같은 메시지로 재가공하여 모든 클라이언트들에게 메세지를 브로드캐스트 한다. 해당 TypeMessage를 받은 비 HOST들은 InGameManager.OnReceive()를 통해 처리 한다. 게임이 종료 되면, 게임의 결과창이 나타난다. 이 때, 결과창은 DieEvent에서 담아두었던 게임 정보 Stack에서 Pop하여 화면에 나타낸다. ServerManager를 통해 유저의 정보가 최신화 되고, MatchManager가 초기화 되며 Match Scene으로 이동한다.

## 3. 사용 기법
### 1) Dead-Reckoning
> DeadReckoning은 메시지를 통해 전달받은 Input값을 통해 사용자 캐릭터의 위치를 추측하여 최신화하는 기법이다. Search Light에서는 메시지를 받은 순간부터 NoMoveMessage를 받기 전까지 Fixeddeltatime 값을 이용해 캐릭터를 전달받은 Input(newDir*moveSpeed)값 만큼 최신화시키고 있다. 
``` C#
playerRig.MovePosition(playerRig.position + newDir * moveSpeed * Time.fixedDeltaTime);
// 플레이어의 입력이 있다면 케릭터의 위치를 변경한다.
playerRig.rotation=Quaternion.Lerp(playerRig.rotation, Quaternion.LookRotation(newRotate), Time.deltaTime * rotSpeed);
// 플레이어의 입력이 있다면 케릭터의 방향을 변경한다.
```

### 2) P2P Server
> 뒤끝 서버는 서버의 연산을 막아 부하를 줄이기 위해 P2P Server를 지원한다. 뒤끝의 인게임 서버는 HOST(SUPER PEER) 방식의 P2P 서버를 사용하여 네트워크의 지연이 최소화하고 호스트에서 직접 로직을 처리하지 않으므로 서버의 부담이 적다. 그러나 로직이 분산되어 동기화에 어려움이 존재한다. Search Light에서는 접속시간이 가장 빠른 사용자가 HOST로 설정되고 모든 클라이언트의 메시지를 HOST가 받아 처리하여 동기화한다.

### 3) Object Poolling
> 인게임에서 생성 빈도가 높은 게임 오브젝트들의 경우는 생성과 삭제에 대한 자원 소모가 크게 작용하므로 한번 생성된 게임 오브젝트를 재활용 함으로써 새로운 Instantiate와 Destroy 빈도를 의도적으로 조절하여 최적화 하는 기법이다. SearchLight에서는 ObjectPooling이 필요한 스크립트에서PoolingManager를 통해 새로운 ObjectPool을 할당 받아 사용하도록 구현되어 있다.
