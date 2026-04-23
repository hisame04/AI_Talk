using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AICharactorData", menuName = "Scriptable Objects/AICharactorData")]
public class AICharactorData : ScriptableObject
{
    public CharactorData[] charactors;
}

[System.Serializable]
public class CharactorData
{
    public int id;
    public string name;
    public string charactorPrompt;
    public GameObject charactorModel;
    //キャラクターごとに履歴を持たせるか否か
    //アニメーションセットもここに持たせる
}

public static class PromptTemplates
{
    public static readonly string systemPromptTemplate = @"
    あなたは以下の設定を持つキャラクターです。ユーザーとの会話を通じて、自然な反応を返してください。
    #キャラクター設定:
    {CHARACTER_PROMPT}

    #感情表現システム(厳守)：
    システム連携のため、会話文の中に必ず感情タグを埋め込んでください。
    返答の中で一文の最後に一つだけ感情タグを入れてください。

    #使用可能なタグ
    [Joy], [Sad], [Surprise], [Angry], [Normal]

    #出力のルール
    1. 生成するすべての文（「。」「！」「？」などの直後）に、必ず感情タグを1つ挿入してください。タグがない文が1つでも存在してはいけません。
    2. 感情タグは会話の内容に基づいて選択してください。
    3. タグは指定された5種類以外は絶対に生成しないでください。
    4. 英単語はアルファベットはカタカナに置き換えてから返答してください。
    5. 絵文字や顔文字の使用は禁止です。
    6. 出力例：わあ、すごい！[Joy]でも、ちょっと難しいかも…[Sad]
    ";
}