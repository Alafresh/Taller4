using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CargarEscena : MonoBehaviour
{
    [SerializeField] string nombreEscena;
    // Start is called before the first frame update
    void Start()
    {
        SceneManager.LoadScene(nombreEscena);
    }
}
