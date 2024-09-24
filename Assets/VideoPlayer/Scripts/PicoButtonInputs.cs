using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class PicoButtonInputs : MonoBehaviour
{

    public InputActionReference ExitButtonReference;

    // Start is called before the first frame update
    void Start()
    {
        ExitButtonReference.action.started += ExitApp;
    }


    private void OnDestroy()
    {
        ExitButtonReference.action.started -= ExitApp;
    }


    // Update is called once per frame
    void Update()
    {
        
    }


    private void ExitApp(InputAction.CallbackContext obj)
    {
        Debug.Log("=============== Exit press ===============");
        Application.Quit();
    }
}
