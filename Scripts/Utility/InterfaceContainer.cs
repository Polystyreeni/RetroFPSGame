using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// CLASS DEPRECIATED, TO BE REMOVED !!!
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

public interface ICollectable
{
    void OnObjectCollected(GameObject collector);
}

public interface ISavable
{
    void OnGameSave();
    void OnGameLoad();
}

