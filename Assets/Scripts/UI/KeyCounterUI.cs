using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class KeyCounterUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI keyCountText;

    private int keyCount = 0;

    void Start()
    {
        UpdateKeyCountText();
    }

    void Update()
    {
        // Temporary test input
        if (Input.GetKeyDown(KeyCode.K))
        {
            AddKey();
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            RemoveKey();
        }
    }

    public void AddKey()
    {
        keyCount++;
        UpdateKeyCountText();
    }

    public void RemoveKey()
    {
        if (keyCount > 0)
        {
            keyCount--;
            UpdateKeyCountText();
        }
    }

    public bool HasKey()
    {
        return keyCount > 0;
    }

    private void UpdateKeyCountText()
    {
        keyCountText.text = "Keys: " + keyCount;
    }
}