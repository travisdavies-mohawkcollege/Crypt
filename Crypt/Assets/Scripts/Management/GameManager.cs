using UnityEngine;

// Marks the root manager hierarchy as persistent across gameplay scene changes.
public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

}
