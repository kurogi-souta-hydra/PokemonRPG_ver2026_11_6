using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("移動")]
    [SerializeField] float moveSpeed = 8f;

    [Header("ジャンプ")]
    [SerializeField] float jumpForce = 2f;

    [Header("接地判定")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.4f;
    [SerializeField] LayerMask groundLayer;

    [Header("すり抜け足場")]
    [SerializeField] LayerMask PlatformLayer;

    [Header("壁判定")]
    [SerializeField] LayerMask wallLayer;
    [SerializeField] float wallCheckDistance = 0.6f;

    [Header("壁キック")]
    [SerializeField] float wallJumpForce = 8f;
    [SerializeField] float wallJumpUpForce = 10f;

    [Header("コントローラー")]
    [SerializeField] float stickDeadZone = 0.2f;

    [Header("プラットフォーム降下")]
    [SerializeField] float doubleTapTime = 0.3f;

    [Header("壁キック設定")]
    [SerializeField] float wallJumpLockTime = 0.15f;
    
    [Header("テレポーター")]
    [SerializeField] GameObject teleporterPrefab;
    [SerializeField] Vector3 teleporterOffset = new Vector3(2f, 0f, 0f);


    // ========================================
    // 変数
    // ========================================

    private float lastDownKeyTime = -1f;

    Rigidbody2D rb;
    Animator animator;

    bool isGrounded;
    bool isTouchingWall;

    // -1 = 左壁
    //  1 = 右壁
    int wallSide;

    // 最後に蹴った壁
    // -1 = 左壁を蹴った
    //  1 = 右壁を蹴った
    //  0 = まだ蹴っていない
    int lastWallJumpSide = 0;

    // 壁キック直後の移動制限
    float wallJumpLockTimer;

    float move;


    // ========================================
    // Awake
    // ========================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }


    // ========================================
    // Start
    // ========================================

    private void Start()
    {
        // ========================================
	    // 5体撃破後 → 最初の場所へ
	    // ========================================

	    if (PlayerPrefs.GetInt("ReturnFirstArea", 0) == 1)
	    {
	        transform.position = new Vector3(0f, 0f, 0f);

	        PlayerPrefs.DeleteKey("ReturnFirstArea");
	        PlayerPrefs.Save();
	    }
	    // ========================================
	    // テレポーターで次のエリアへ
	    // ========================================
	    else if (PlayerPrefs.GetInt("NextArea", 0) == 1)
	    {
	        transform.position = new Vector3(22f, 181f, 0f);

	        PlayerPrefs.SetInt("NextArea", 0);
	        PlayerPrefs.Save();
	    }

	    // ========================================
	    // 戦闘前の場所へ戻る
	    // ========================================

	    else if (PlayerPrefs.HasKey("PlayerX"))
	    {
	        float x = PlayerPrefs.GetFloat("PlayerX");
	        float y = PlayerPrefs.GetFloat("PlayerY");

	        transform.position = new Vector3(x, y, 0);
	    }
	    
	    // ========================================
		// 4体撃破後 → テレポーター設置
		// ========================================

		if (PlayerPrefs.GetInt("TeleportCreated", 0) == 1)
		{
		    if (teleporterPrefab != null)
		    {
		        Instantiate(
		            teleporterPrefab,
		            transform.position + teleporterOffset,
		            Quaternion.identity
		        );

		        PlayerPrefs.SetInt("TeleportCreated", 0);
		        PlayerPrefs.Save();

		        Debug.Log("テレポーターを設置しました");
		    }
		    else
		    {
		        Debug.LogError("テレポーターPrefabが設定されていません");
		    }
		}
	    
        // ゲーム開始時は壁キック履歴をリセット
        lastWallJumpSide = 0;
    }


    // ========================================
    // Update
    // ========================================

    private void Update()
    {
        // ========================================
        // 接地判定
        // ========================================

        CheckGround();


        // ========================================
        // 壁判定
        // ========================================

        CheckWall();


        // ========================================
        // 壁キックロック時間
        // ========================================

        if (wallJumpLockTimer > 0f)
        {
            wallJumpLockTimer -= Time.deltaTime;
        }


        // ========================================
        // 地面に着いたら壁キック履歴をリセット
        // ========================================

        if (isGrounded)
        {
            lastWallJumpSide = 0;
        }


        // ========================================
        // 移動入力
        // ========================================

        move = 0f;


        // ----------------------------------------
        // キーボード
        // ----------------------------------------

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed)
            {
                move = -1f;

                animator.SetFloat("moveX", -1);
                animator.SetFloat("moveY", 0);
            }
            else if (Keyboard.current.dKey.isPressed ||
                     Keyboard.current.rightArrowKey.isPressed)
            {
                move = 1f;

                animator.SetFloat("moveX", 1);
                animator.SetFloat("moveY", 0);
            }
        }


        // ----------------------------------------
        // コントローラー
        // ----------------------------------------

        Gamepad gamepad = Gamepad.current;

        if (gamepad != null)
        {
            float stickX = gamepad.leftStick.x.ReadValue();

            if (Mathf.Abs(stickX) > stickDeadZone)
            {
                move = stickX;

                if (stickX < 0)
                {
                    animator.SetFloat("moveX", -1);
                    animator.SetFloat("moveY", 0);
                }
                else
                {
                    animator.SetFloat("moveX", 1);
                    animator.SetFloat("moveY", 0);
                }
            }
        }


        // ========================================
        // 歩きアニメーション
        // ========================================

        animator.SetBool(
            "isMoving",
            Mathf.Abs(move) > 0.01f
        );


        // ========================================
        // ジャンプ入力
        // ========================================

        bool jumpPressed = false;
        bool jumpHeld = false;

        // キーボード
        if (Keyboard.current != null)
        {
            jumpPressed =
                Keyboard.current.wKey.wasPressedThisFrame ||
                Keyboard.current.upArrowKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame;

            jumpHeld =
                Keyboard.current.wKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed ||
                Keyboard.current.spaceKey.isPressed;
        }

        // コントローラー
        Gamepad pad = Gamepad.current;

        if (pad != null)
        {
            if (pad.buttonSouth.wasPressedThisFrame)
            {
                jumpPressed = true;
            }

            if (pad.buttonSouth.isPressed)
            {
                jumpHeld = true;
            }
        }

        // ========================================
        // 通常ジャンプ・連続ジャンプ・壁キック
        // ========================================

        // ボタンを押した瞬間
        if (jumpPressed)
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    jumpForce
                );

                lastWallJumpSide = 0;

                Debug.Log("通常ジャンプ");
            }
            else if (isTouchingWall)
            {
                WallJump();
            }
        }
        // ボタン長押し中、地面に着いたら再ジャンプ
        else if (jumpHeld && isGrounded)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );
        }



        // ========================================
        // 下方向 + ボタンでプラットフォームを降りる
        // ========================================

        bool downInput = false;


        // ----------------------------------------
        // キーボード
        // ----------------------------------------

        if (Keyboard.current != null &&
            (Keyboard.current.sKey.wasPressedThisFrame ||
             Keyboard.current.downArrowKey.wasPressedThisFrame))
        {
            downInput = true;
        }


        // ----------------------------------------
        // コントローラー
        // ----------------------------------------

        Gamepad padForDown = Gamepad.current;

        if (padForDown != null)
        {
            Vector2 leftStick =
                padForDown.leftStick.ReadValue();

            bool stickDown =
                leftStick.y < -stickDeadZone;

            bool downButton =
                padForDown.buttonSouth.wasPressedThisFrame;

            if (stickDown && downButton)
            {
                downInput = true;
            }
        }


        // ----------------------------------------
        // 下入力
        // ----------------------------------------

        if (downInput)
        {
            if (Time.time - lastDownKeyTime <= doubleTapTime)
            {
                DescendPlatform();
            }

            lastDownKeyTime = Time.time;
        }


        // ========================================
        // デバッグ用：Pキー
        // ========================================

        if (Keyboard.current != null &&
            Keyboard.current.pKey.wasPressedThisFrame)
        {
            PlayerPrefs.DeleteAll();

            Debug.Log("セーブデータをリセットしました");

            SceneManager.LoadScene("Title Scene");
        }
    }


    // ========================================
    // 接地判定
    // ========================================

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer | PlatformLayer
        );
    }


    // ========================================
    // 壁判定
    // ========================================

    private void CheckWall()
    {
        isTouchingWall = false;
        wallSide = 0;


        // ----------------------------------------
        // 地面にいる場合は壁キック不可
        // ----------------------------------------

        if (isGrounded)
        {
            return;
        }


        // ----------------------------------------
        // 左側の壁をチェック
        // ----------------------------------------

        RaycastHit2D leftWall = Physics2D.Raycast(
            transform.position,
            Vector2.left,
            wallCheckDistance,
            wallLayer
        );


        // ----------------------------------------
        // 右側の壁をチェック
        // ----------------------------------------

        RaycastHit2D rightWall = Physics2D.Raycast(
            transform.position,
            Vector2.right,
            wallCheckDistance,
            wallLayer
        );


        // ----------------------------------------
        // 左壁
        // ----------------------------------------

        if (leftWall.collider != null)
        {
            isTouchingWall = true;
            wallSide = -1;
        }


        // ----------------------------------------
        // 右壁
        // ----------------------------------------

        else if (rightWall.collider != null)
        {
            isTouchingWall = true;
            wallSide = 1;
        }
    }


    // ========================================
    // 壁キック
    // ========================================

    private void WallJump()
    {
        // ----------------------------------------
        // 同じ壁を連続して蹴るのを禁止
        // ----------------------------------------

        if (wallSide == lastWallJumpSide)
        {
            Debug.Log("同じ壁なので壁キックできません");
            return;
        }


        // ----------------------------------------
        // 壁と反対方向
        // ----------------------------------------

        float jumpDirection = -wallSide;


        // ----------------------------------------
        // 壁から離れる方向へ飛ぶ
        // ----------------------------------------

        rb.linearVelocity = new Vector2(
            jumpDirection * wallJumpForce,
            wallJumpUpForce
        );


        // ----------------------------------------
        // 今回蹴った壁を記録
        // ----------------------------------------

        lastWallJumpSide = wallSide;


        // ----------------------------------------
        // 壁キック直後の通常移動を少し止める
        // ----------------------------------------

        wallJumpLockTimer = wallJumpLockTime;


        Debug.Log(
            "壁キック！ 蹴った壁：" +
            (wallSide == -1 ? "左" : "右")
        );
    }


    // ========================================
    // FixedUpdate
    // ========================================

    private void FixedUpdate()
    {
        // ========================================
        // 横移動
        // ========================================

        // 壁キック直後は、
        // 入力によってX速度を上書きしない
        if (wallJumpLockTimer > 0f)
        {
            return;
        }


        rb.linearVelocity = new Vector2(
            move * moveSpeed,
            rb.linearVelocity.y
        );
    }


    // ========================================
    // 敵と接触
    // ========================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("モンスターに遭遇");


            // ------------------------------------
            // 敵からプレイヤーへの方向
            // ------------------------------------

            Vector2 dir =
                (transform.position - other.transform.position)
                .normalized;


            // ------------------------------------
            // 敵から2m離れた位置
            // ------------------------------------

            Vector2 respawnPos =
                (Vector2)transform.position + dir * 2f;


            // ------------------------------------
            // 戻ってきたときの位置を保存
            // ------------------------------------

            PlayerPrefs.SetFloat(
                "PlayerX",
                respawnPos.x
            );

            PlayerPrefs.SetFloat(
                "PlayerY",
                respawnPos.y
            );

            PlayerPrefs.Save();


            // ------------------------------------
            // 敵データ取得
            // ------------------------------------

            Enemy enemy =
                other.GetComponent<Enemy>();


            if (enemy != null)
            {
                BattleSystem.EnemyBase =
                    enemy.pokemonBase;

                BattleSystem.EnemyLevel =
                    enemy.level;

                BattleSystem.EnemyID =
                    enemy.enemyID;


                // --------------------------------
                // バトルシーンへ
                // --------------------------------

                SceneManager.LoadScene(
                    "Battle Scene"
                );
            }
        }
    }


    // ========================================
    // すり抜け床から降りる
    // ========================================

    private void DescendPlatform()
    {
        Debug.Log("DescendPlatform");


        float radius = 0.5f;


        Collider2D[] platforms =
            Physics2D.OverlapCircleAll(
                groundCheck.position,
                radius,
                PlatformLayer
            );


        Debug.Log(
            "見つかった足場：" +
            platforms.Length
        );


        foreach (Collider2D platform in platforms)
        {
            Debug.Log(
                "Collider : " +
                platform.name
            );


            Platform platformScript =
                platform.GetComponent<Platform>();


            Debug.Log(
                "Platform : " +
                platformScript
            );


            if (platformScript != null)
            {
                platformScript.DisablePlatform();
            }
        }
    }


    // ========================================
    // 接地判定・壁判定の可視化
    // ========================================

    private void OnDrawGizmosSelected()
    {
        // ----------------------------------------
        // 接地判定
        // ----------------------------------------

        if (groundCheck != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }


        // ----------------------------------------
        // 左右の壁判定
        // ----------------------------------------

        Gizmos.color = Color.blue;


        Gizmos.DrawLine(
            transform.position,
            transform.position +
            Vector3.left * wallCheckDistance
        );


        Gizmos.DrawLine(
            transform.position,
            transform.position +
            Vector3.right * wallCheckDistance
        );
    }
}