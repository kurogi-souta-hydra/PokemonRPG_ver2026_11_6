using UnityEngine; // Unityの基本機能
using UnityEngine.SceneManagement; // シーン管理機能
using System.Collections; // コルーチン、時間を少し待つ
using System.Collections.Generic; // 型指定できるコレクション
using UnityEngine.InputSystem; // 新しいInput System


public enum BattleState
{
    Start,
    PlayerAction, // 行動選択
    PlayerMove,   // 技選択
    EnemyMove,
    Busy,
}


public class BattleSystem : MonoBehaviour
{
    public static PokemonBase EnemyBase;
    public static int EnemyLevel;
    public static string EnemyID;

    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleHud playerHud;
    [SerializeField] BattleHud enemyHud;
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] BattleBackground battleBackground;
    [SerializeField] FadeManager fadeManager;


    BattleState state;

    int currentAction = 0; // 0:Fight, 1:Run
    int currentMove = 0;   // 0:左上, 1:右上, 2:左下, 3:右下


    // 効果音を使用します
    public AudioClip sound1;      // 攻撃音・被弾音
    public AudioClip faintSound;  // 消滅音

    AudioSource audioSource;


    private void Start()
    {
        StartCoroutine(SetupBattle());

        // Componentを取得
        audioSource = GetComponent<AudioSource>();
    }


    IEnumerator SetupBattle()
    {
        if (EnemyBase == null)
        {
            Debug.LogError("EnemyBaseが設定されていません");
            SceneManager.LoadScene("Map Scene");
            yield break;
        }

        Debug.Log(EnemyBase.name);
        Debug.Log(EnemyLevel);


        state = BattleState.Start;


        // モンスターの生成と描画
        playerUnit.Setup();
        enemyUnit.Setup(EnemyBase, EnemyLevel);

        battleBackground.SetBackground(EnemyID);


        // HUDの描画
        playerHud.SetData(playerUnit.Pokemon);
        enemyHud.SetData(enemyUnit.Pokemon);


        dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);


        yield return dialogBox.TypeDialog(
            $"やせいの{enemyUnit.Pokemon.Base.Name}があらわれた"
        );


        PlayerAction();
    }


    // ==========================================
    // 行動選択
    // ==========================================

    void PlayerAction()
    {
        state = BattleState.PlayerAction;

        dialogBox.EnableActionSelector(true);

        StartCoroutine(dialogBox.TypeDialog("どうする？"));
    }


    // ==========================================
    // 技選択
    // ==========================================

    void PlayerMove()
    {
        state = BattleState.PlayerMove;

        dialogBox.EnableDialogText(false);
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableMoveSelector(true);
    }


    // ==========================================
    // プレイヤーの技実行
    // ==========================================

    IEnumerator PerformPlayerMove()
    {
        state = BattleState.Busy;


        Move move = playerUnit.Pokemon.Moves[currentMove];


        // PPがないなら技を使えない
        if (move.PP <= 0)
        {
            yield return dialogBox.TypeDialog("PPがたりない！");

            yield return EnemyMove();

            yield break;
        }


        // PPを1減らす
        move.PP--;


        yield return dialogBox.TypeDialog(
            $"{playerUnit.Pokemon.Base.Name}は{move.Base.Name}をつかった"
        );


        playerUnit.PlayerAttackAnimation();

        // 攻撃音
        audioSource.PlayOneShot(sound1);


        yield return new WaitForSeconds(0.7f);


        enemyUnit.PlayerHitAnimation();

        // 被弾音
        audioSource.PlayOneShot(sound1);


        DamageDetails damageDetails = null;


        // ==========================================
        // 攻撃技ならダメージを与える
        // ==========================================

        if (move.Base.Power > 0)
        {
            damageDetails =
                enemyUnit.Pokemon.TakeDamage(
                    move,
                    playerUnit.Pokemon
                );


            yield return enemyHud.UpdateHP();

            yield return ShowDamageDetails(damageDetails);


            // 敵モンスターを倒したら
            if (damageDetails.Fainted)
            {
                yield return dialogBox.TypeDialog(
                    $"{enemyUnit.Pokemon.Base.Name}は戦闘不能"
                );


                // 敵の消滅音
                audioSource.PlayOneShot(faintSound);

                // 敵の消滅アニメーション
                enemyUnit.PlayerFaintAnimation();

                yield return new WaitForSeconds(1f);


                BattleResult.isVictory = true;


                Debug.Log("保存するID：" + EnemyID);


				// 敵撃破処理
				if (PlayerPrefs.GetInt(EnemyID, 0) == 0)
				{
				    PlayerPrefs.SetInt(EnemyID, 1);

				    int count = PlayerPrefs.GetInt("DefeatedCount", 0);
				    count++;

				    PlayerPrefs.SetInt("DefeatedCount", count);

				    Debug.Log("倒した数：" + count);

				    // グレースケール撃破数を加算
				    GrayscaleManager manager =
				        FindFirstObjectByType<GrayscaleManager>();

				    if (manager != null)
				    {
				        manager.EnemyDefeated();
				    }
				}
                PlayerPrefs.Save();


                int defeatedCount =
                    PlayerPrefs.GetInt("DefeatedCount", 0);


                // ==========================================
                // 5体撃破
                // ==========================================
                if (defeatedCount >= 5)
                {
                    Debug.Log("5体撃破！最初の場所へ");
                    PlayerPrefs.SetInt("ReturnFirstArea", 1);
                    PlayerPrefs.Save();
                    
                    yield return fadeManager.FadeOutStart(); // フェードアウト
                    SceneManager.LoadScene("Map Scene");
                    yield break;
                }

                // ==========================================
                // 4体撃破
                // ==========================================
                else if (defeatedCount >= 4)
                {
                    Debug.Log("4体撃破！テレポーター設置");
				    PlayerPrefs.SetInt("TeleportCreated", 1);
				    PlayerPrefs.SetInt("NextArea", 0);
				    PlayerPrefs.Save();
                }

                yield return fadeManager.FadeOutStart();
                SceneManager.LoadScene("Map Scene");
                yield break;
            }
        }


        // ==========================================
        // 回復技なら回復する
        // ==========================================

        if (move.Base.Heal > 0)
        {
            playerUnit.Pokemon.Heal(move.Base.Heal);

            yield return playerHud.UpdateHP();
        }


        // 敵のターン
        yield return EnemyMove();
    }


    // ==========================================
    // 敵の技実行
    // ==========================================

    IEnumerator EnemyMove()
    {
        state = BattleState.EnemyMove;


        // 技を決定：ランダム
        Move move =
            enemyUnit.Pokemon.GetRandomMove();


        // PPがないなら技を使えない
        if (move.PP <= 0)
        {
            yield return dialogBox.TypeDialog(
                "PPがたりない！"
            );


            yield return EnemyMove();


            yield break;
        }


        // PPを1減らす
        move.PP--;


        yield return dialogBox.TypeDialog(
            $"{enemyUnit.Pokemon.Base.Name}は{move.Base.Name}をつかった"
        );


        enemyUnit.PlayerAttackAnimation();

        // 攻撃音
        audioSource.PlayOneShot(sound1);


        yield return new WaitForSeconds(0.7f);


        playerUnit.PlayerHitAnimation();

        // 被弾音
        audioSource.PlayOneShot(sound1);


        DamageDetails damageDetails = null;


        // ==========================================
        // 攻撃技ならダメージを与える
        // ==========================================

        if (move.Base.Power > 0)
        {
            damageDetails =
                playerUnit.Pokemon.TakeDamage(
                    move,
                    enemyUnit.Pokemon
                );


            yield return playerHud.UpdateHP();

            yield return ShowDamageDetails(
                damageDetails
            );


            // プレイヤーが倒されたら
            if (damageDetails.Fainted)
            {
                yield return dialogBox.TypeDialog(
                    $"{playerUnit.Pokemon.Base.Name}は戦闘不能"
                );


                // プレイヤーの消滅音
                audioSource.PlayOneShot(faintSound);

                // 消滅アニメーション
                playerUnit.PlayerFaintAnimation();

                yield return new WaitForSeconds(1f);


                BattleResult.isVictory = false;


                int defeatedCount =
                    PlayerPrefs.GetInt(
                        "DefeatedCount",
                        0
                    );


                if (defeatedCount >= 4)
                {
                    Debug.Log("4体撃破！次の場所へ");


                    PlayerPrefs.SetInt("NextArea",1);

                    PlayerPrefs.Save();
                }


                yield return fadeManager.FadeOutStart();


                SceneManager.LoadScene(
                    "Map Scene"
                );


                yield break;
            }
        }


        // ==========================================
        // 回復技なら回復する
        // ==========================================

        if (move.Base.Heal > 0)
        {
            enemyUnit.Pokemon.Heal(move.Base.Heal);

            yield return enemyHud.UpdateHP();
        }


        // プレイヤーの行動選択へ
        PlayerAction();
    }


    // ==========================================
    // ダメージ詳細
    // ==========================================

    IEnumerator ShowDamageDetails(
        DamageDetails damageDetails
    )
    {
        if (damageDetails.Critical > 1f)
        {
            yield return dialogBox.TypeDialog(
                "急所にあたった"
            );
        }


        if (damageDetails.TypeEffectiveness > 1f)
        {
            yield return dialogBox.TypeDialog(
                "効果はバツグンだ"
            );
        }
        else if (damageDetails.TypeEffectiveness < 1f)
        {
            yield return dialogBox.TypeDialog(
                "効果はいまひとつ"
            );
        }
    }


    // ==========================================
    // 逃げる
    // ==========================================

    IEnumerator RunAway()
    {
        state = BattleState.Busy;


        if (Random.Range(0, 100) < 50)
        {
            yield return dialogBox.TypeDialog(
                "うまく にげきれた!"
            );


            yield return fadeManager.FadeOutStart();


            yield return new WaitForSeconds(2f);


            SceneManager.LoadScene(
                "Map Scene"
            );
        }
        else
        {
            yield return dialogBox.TypeDialog(
                "にげられなかった!"
            );


            yield return EnemyMove();
        }
    }


    // ==========================================
    // Update
    // ==========================================

    private void Update()
    {
        Gamepad gamepad = Gamepad.current;
        
        // ========================================
		// デバッグ用：Pキー
		// セーブデータをリセットしてタイトルへ
		// ========================================

		if (Keyboard.current != null &&
		    Keyboard.current.pKey.wasPressedThisFrame)
		{
		    PlayerPrefs.DeleteAll();

		    Debug.Log("セーブデータをリセットしました");

		    SceneManager.LoadScene("Title Scene");

		    return;
		}


        // ==========================================
        // 戻るボタン
        // キーボード：Esc
        // コントローラー：B / ○
        // ==========================================

        bool backPressed =
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);


        if (gamepad != null)
        {
            if (gamepad.buttonEast.wasPressedThisFrame)
            {
                backPressed = true;
            }
        }


        if (backPressed)
        {
            HandleBack();
        }


        // ==========================================
        // 行動選択
        // ==========================================

        if (state == BattleState.PlayerAction)
        {
            HandleActionSelection();
        }


        // ==========================================
        // 技選択
        // ==========================================

        else if (state == BattleState.PlayerMove)
        {
            HandleMoveSelection();
        }
    }


    // ==========================================
    // 戻る
    // ==========================================

    void HandleBack()
    {
        if (state == BattleState.PlayerMove)
        {
            // 技の選択位置を初期化
            currentMove = 0;


            // 技選択を終了
            dialogBox.EnableMoveSelector(false);


            // 行動選択を表示
            dialogBox.EnableActionSelector(true);


            // メッセージ表示を戻す
            dialogBox.EnableDialogText(true);


            state = BattleState.PlayerAction;


            StartCoroutine(
                dialogBox.TypeDialog("どうする？")
            );
        }
    }


    // ==========================================
    // 行動選択
    // ==========================================

    void HandleActionSelection()
    {
        Gamepad gamepad = Gamepad.current;


        // ==========================================
        // 上下入力
        // ==========================================

        bool downPressed =
            (Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.downArrowKey.wasPressedThisFrame);


        bool upPressed =
            (Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame);


        // コントローラー
        if (gamepad != null)
        {
            if (gamepad.leftStick.down.wasPressedThisFrame)
            {
                downPressed = true;
            }


            if (gamepad.leftStick.up.wasPressedThisFrame)
            {
                upPressed = true;
            }
        }


        // ==========================================
        // 下
        // ==========================================

        if (downPressed)
        {
            if (currentAction < 1)
            {
                currentAction++;
            }
        }


        // ==========================================
        // 上
        // ==========================================

        if (upPressed)
        {
            if (currentAction > 0)
            {
                currentAction--;
            }
        }


        dialogBox.UpdateActionSelection(
            currentAction
        );


        // ==========================================
        // 決定
        // キーボード：Space / Enter
        // コントローラー：A / ×
        // ==========================================

        bool submitPressed =
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.numpadEnterKey.wasPressedThisFrame) ;


        if (gamepad != null)
        {
            if (gamepad.buttonSouth.wasPressedThisFrame)
            {
                submitPressed = true;
            }
        }


        if (submitPressed)
        {
            // たたかう
            if (currentAction == 0)
            {
                PlayerMove();
            }


            // にげる
            else if (currentAction == 1)
            {
                StartCoroutine(
                    RunAway()
                );
            }
        }
    }


    // ==========================================
    // 技選択
    //
    // 0：左上
    // 1：右上
    // 2：左下
    // 3：右下
    // ==========================================

    void HandleMoveSelection()
    {
        Gamepad gamepad = Gamepad.current;


        // ==========================================
        // 入力判定
        // ==========================================

        bool rightPressed =
            (Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.rightArrowKey.wasPressedThisFrame);


        bool leftPressed =
            (Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.leftArrowKey.wasPressedThisFrame);


        bool downPressed =
            (Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.downArrowKey.wasPressedThisFrame);


        bool upPressed =
            (Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame);


        // コントローラー
        if (gamepad != null)
        {
            if (gamepad.leftStick.right.wasPressedThisFrame)
            {
                rightPressed = true;
            }


            if (gamepad.leftStick.left.wasPressedThisFrame)
            {
                leftPressed = true;
            }


            if (gamepad.leftStick.down.wasPressedThisFrame)
            {
                downPressed = true;
            }


            if (gamepad.leftStick.up.wasPressedThisFrame)
            {
                upPressed = true;
            }
        }


        // ==========================================
        // 右
        // ==========================================

        if (rightPressed)
        {
            if (
                currentMove <
                playerUnit.Pokemon.Moves.Count - 1
            )
            {
                currentMove++;
            }
        }


        // ==========================================
        // 左
        // ==========================================

        else if (leftPressed)
        {
            if (currentMove > 0)
            {
                currentMove--;
            }
        }


        // ==========================================
        // 下
        // ==========================================

        else if (downPressed)
        {
            if (
                currentMove <
                playerUnit.Pokemon.Moves.Count - 2
            )
            {
                currentMove += 2;
            }
        }


        // ==========================================
        // 上
        // ==========================================

        else if (upPressed)
        {
            if (currentMove > 1)
            {
                currentMove -= 2;
            }
        }


        // ==========================================
        // 技選択UI更新
        // ==========================================

        dialogBox.UpdateMoveSelection(
            currentMove,
            playerUnit.Pokemon.Moves[currentMove]
        );


        // ==========================================
        // 決定
        // キーボード：Space / Enter
        // コントローラー：A / ×
        // ==========================================

        bool submitPressed =
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.numpadEnterKey.wasPressedThisFrame) ;


        if (gamepad != null)
        {
            if (gamepad.buttonSouth.wasPressedThisFrame)
            {
                submitPressed = true;
            }
        }


        if (submitPressed)
        {
            // 技選択のUIを非表示
            dialogBox.EnableMoveSelector(false);


            // メッセージ復活
            dialogBox.EnableDialogText(true);


            // 技決定
            StartCoroutine(
                PerformPlayerMove()
            );
        }
    }
}