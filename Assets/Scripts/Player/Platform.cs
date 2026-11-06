using System;
using System.Collections;
using UnityEngine;

public class Platform : MonoBehaviour
{
    /// <summary>
    /// 足場として使用するコライダー
    /// </summary>
    [SerializeField] private BoxCollider2D boxCollider;

    /// <summary>
    /// ゲーム開始時の初期化
    /// </summary>
    private void Start()
    {
        boxCollider.enabled = true;
    }

    /// <summary>
    /// 足場を一時的に無効化する
    /// </summary>
    public void DisablePlatform()
    {
        // すでに無効なら何もしない
        if (!boxCollider.enabled) return;

        // 足場を無効化
        boxCollider.enabled = false;

        // 一定時間後に足場を有効化する
        StartCoroutine(WaitProcess(1.2f, () => boxCollider.enabled = true));
    }

    /// <summary>
    /// 指定時間待機してから処理を実行するコルーチン
    /// </summary>
    /// <param name="time">待機時間（秒）</param>
    /// <param name="action">待機後に実行する処理</param>
    /// <returns>コルーチン</returns>
    private IEnumerator WaitProcess(float time, Action action)
    {
        yield return new WaitForSeconds(time);

        action();
    }

    /// <summary>
    /// プレイヤーが足場の下側のTriggerに入ったとき
    /// </summary>
    /// <param name="col">衝突したコライダー</param>
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            // プレイヤーが下から来たので足場を無効化
            boxCollider.enabled = false;
        }
    }

    /// <summary>
    /// プレイヤーがTriggerから出たとき
    /// </summary>
    /// <param name="col">衝突したコライダー</param>
    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            // プレイヤーが足場を抜けたので有効化
            boxCollider.enabled = true;
        }
    }
}