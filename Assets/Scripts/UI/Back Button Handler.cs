using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMenu : MonoBehaviour
{
    public async void LeaveServer()
    {
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
                Debug.Log("Player signed out.");
            }

            NetworkManager.Singleton.Shutdown();
            PauseScreen.isPause = false;
            Debug.Log("NetworkManager shut down.");

            await Task.Delay(1000);

            SceneManager.LoadScene("Menu");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while leaving server: {ex.Message}");
        }
    }
}
