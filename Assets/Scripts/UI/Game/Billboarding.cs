using UnityEngine;

public class Billboarding : MonoBehaviour
{
    #region Variáveis
    // VARIÁVEIS OCULTAS
    private Camera _mainCamera;
    #endregion

    #region Funções Unity
    /* - - - - -
     * 
     * - - - - - */
    private void Start()
    {
        _mainCamera = Camera.main;
    }

    /* - - - - -
     * 
     * - - - - - */
    private void Update()
    {
        Quaternion rotation = _mainCamera.transform.rotation;
        transform.LookAt(transform.position + rotation * Vector3.forward, rotation * Vector3.up);
    }
    #endregion
}