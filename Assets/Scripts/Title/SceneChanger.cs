using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SceneChanger : MonoBehaviour
{
    public void LoadMap()
    {
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene("Map Scene");
    }

    void Update()
    {
        // Enterキー or テンキーEnter or Spaceキーが押された瞬間
        if (Keyboard.current != null &&
        (Keyboard.current.enterKey.wasPressedThisFrame ||
         Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
         Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            LoadMap();
        }
    }
}
