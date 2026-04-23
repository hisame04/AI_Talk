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
    public string charactorSettings;
    public GameObject charactorModel;
    //キャラクターごとに履歴を持たせるか否か
    //アニメーションセットもここに持たせる
}