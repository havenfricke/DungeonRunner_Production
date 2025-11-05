using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class KeyCounterUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI keyCountText;

    private int keyCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        UpdateKeyCountText();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            keyCount++;
            UpdateKeyCountText();
        }

        else if (Input.GetKeyDown(KeyCode.L))
        {
            if (keyCount > 0)
                keyCount--;
            UpdateKeyCountText();
        }
    }

    private void UpdateKeyCountText()
    {
        keyCountText.text = "Keys: " + keyCount;
    }
}
