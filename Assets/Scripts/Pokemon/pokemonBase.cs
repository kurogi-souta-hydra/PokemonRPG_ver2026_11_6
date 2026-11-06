using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ポケモンのマスターデータ：外部から変更しない（インスペクターだけ変更可能）
[CreateAssetMenu]
public class PokemonBase : ScriptableObject
{
    // 名前,説明,画像,タイプ,ステータス
    
    [SerializeField] new string name;
    [TextArea]
    [SerializeField] string deszription;
    
    // 画像
    [SerializeField] Sprite frontSprite;
    [SerializeField] Sprite backSprite;
    
    // タイプ
    [SerializeField] PokemonType type1;
    [SerializeField] PokemonType type2;
    
    // ステータス:hp,at,df,sAT,sDF,sp
    [SerializeField] int maxHP;
    [SerializeField] int attack;
    [SerializeField] int defense;
    [SerializeField] int spAttack;
    [SerializeField] int spDefense;
    [SerializeField] int speed;
    
    // 覚える技一覧
    [SerializeField] List<LearnableMove> learnableMoves;
    
    // 他ファイルからattackの値の取得はできるが変更できない
    public int Attack { get => attack; }
    public int Defense { get => defense; }
    public int SpAttack { get => spAttack; }
    public int SpDefense { get => spDefense; }
    public int Speed { get => speed; }
    public int MaxHP { get => maxHP; }
    
    public List<LearnableMove> LearnableMoves { get => learnableMoves; }
    public String Name { get => name; }
    public String Deszription { get => deszription; }
    public Sprite FrontSprite { get => frontSprite; }
    public Sprite BackSprite { get => backSprite; }
    public PokemonType Type1 { get => type1; }
    public PokemonType Type2 { get => type2; }
    
}

// 覚える技クラス：どのレベルで何を覚えるか
[Serializable]
public class LearnableMove
{
    // ヒエラルキーで設定する
    [SerializeField] MoveBase _base;
    [SerializeField] int level;
    
    public MoveBase Base { get => _base; }
    public int Level { get => level; }
}

public enum PokemonType
{
    None,       // なし
    Normal,     // ノーマル
    Fire,       // ほのお
    Water,      // みず
    Electric,   // でんき
    Grass,      // くさ
    Ice,        // こおり
    Fighting,   // かくとう
    Poison,     // どく
    Ground,     // じめん
    Flying,     // ひこう
    Psychic,    // エスパー
    Bug,        // むし
    Rock,       // いわ
    Ghost,      // ゴースト
    Dragon,     // ドラゴン
    Dark,       // あく
    Steel,      // はがね
    Fairy,      // フェアリー
}


// やりたいこと
// ・わざの相性計算
// ・クリティカルヒット
// ・上のことをメッセージに表示
public class TypeChart
{
    static float[][] chart =
    {
    //攻撃/防御        NOR  FIR  WAT  ELE  GRS  ICE  FIG  POI  GRO  FLY  PSY  BUG  LOC  GHO  DRA  DAR  STE  FAI
    /*NOR*/ new float[]{1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,0.5f,  0f,  1f,  1f,0.5f,  1f},
    /*FIR*/ new float[]{1f,0.5f,0.5f,  1f,  2f,  2f,  1f,  1f,  1f,  1f,  1f,  2f,0.5f,  1f,0.5f,  1f,  2f,  1f},
    /*WAT*/ new float[]{1f,  2f,0.5f,  1f,0.5f,  1f,  1f,  1f,  2f,  1f,  1f,  1f,  2f,  1f,0.5f,  1f,  1f,  1f},
    /*ELE*/ new float[]{1f,  1f,  2f,0.5f,0.5f,  1f,  1f,  1f,  0f,  2f,  1f,  1f,  1f,  1f,0.5f,  1f,  1f,  1f},
    /*GRS*/ new float[]{1f,0.5f,  2f,  1f,0.5f,  1f,  1f,0.5f,  2f,0.5f,  1f,0.5f,  2f,  1f,0.5f,  1f,0.5f,  1f},
    /*ICE*/ new float[]{1f,0.5f,0.5f,  1f,  2f,0.5f,  1f,  1f,  2f,  2f,  1f,  1f,  1f,  1f,  2f,  1f,0.5f,  1f},
    /*FIG*/ new float[]{2f,  1f,  1f,  1f,  1f,  2f,  1f,0.5f,  1f,0.5f,0.5f,0.5f,  2f,  0f,  1f,  2f,  2f,0.5f},
    /*POI*/ new float[]{1f,  1f,  1f,  1f,  2f,  1f,  1f,0.5f,0.5f,  1f,  1f,  1f,0.5f,0.5f,  1f,  1f,  0f,  2f},
    /*GRO*/ new float[]{1f,  2f,  1f,  2f,0.5f,  1f,  1f,  2f,  1f,  0f,  1f,0.5f,  2f,  1f,  1f,  1f,  2f,  1f},
    /*FLY*/ new float[]{1f,  1f,  1f,0.5f,  2f,  1f,  2f,  1f,  1f,  1f,  1f,  2f,0.5f,  1f,  1f,  1f,0.5f,  1f},
    /*PSY*/ new float[]{1f,  1f,  1f,  1f,  1f,  1f,  2f,  2f,  1f,  1f,0.5f,  1f,  1f,  1f,  1f,  0f,0.5f,  1f},
    /*BUG*/ new float[]{1f,0.5f,  1f,  1f,  2f,  1f,0.5f,0.5f,  1f,0.5f,  2f,  1f,  1f,0.5f,  1f,  2f,0.5f,0.5f},
    /*ROC*/ new float[]{1f,  2f,  1f,  1f,  1f,  2f,0.5f,  1f,0.5f,  2f,  1f,  2f,  1f,  0f,  1f,  1f,0.5f,  1f},
    /*GHO*/ new float[]{0f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  2f,  1f,  1f,  2f,  1f,0.5f,  1f,  1f},
    /*DRA*/ new float[]{1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  2f,  1f,0.5f,  0f},
    /*DAR*/ new float[]{1f,  1f,  1f,  1f,  1f,  1f,0.5f,  1f,  1f,  1f,  2f,  1f,  1f,  2f,  1f,0.5f,  1f,0.5f},
    /*STE*/ new float[]{1f,0.5f,0.5f,0.5f,  1f,  2f,  1f,  1f,  1f,  1f,  1f,  1f,  2f,  1f,  1f,  1f,0.5f,  2f},
    /*FAI*/ new float[]{1f,0.5f,  1f,  1f,  1f,  1f,  2f,0.5f,  1f,  1f,  1f,  1f,  1f,  1f,  2f,  2f,0.5f,  1f}
    };
    
    public static float GetEffectivenss(PokemonType attackType, PokemonType defenseType)
    {
        if (attackType == PokemonType.None || defenseType == PokemonType.None)
        {
            return 1f;
        }
        int row = (int)attackType - 1;
        int col = (int)defenseType - 1;
        return chart[row][col];
    }
}
