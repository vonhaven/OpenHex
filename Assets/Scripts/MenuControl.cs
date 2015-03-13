using UnityEngine;
using System.Collections;

public class MenuControl : MonoBehaviour {

    public GameObject helpMenu;

	void Start()
    {
        helpMenu.SetActive(false);
	}
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            helpMenu.SetActive(!helpMenu.activeSelf);
        }
    }
}