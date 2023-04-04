using RTSEngine.Scene;
using UnityEngine;

namespace RTSEngine.Demo
{
	public class MainMenu : MonoBehaviour {

        [SerializeField]
        private bool enableMultiplayer = true;
        [SerializeField]
        private GameObject multiplayerButton = null;
        [SerializeField]
        private GameObject webGLMultiplayerMsg = null;
        [SerializeField]
        private GameObject exitButton = null;

        [SerializeField, Tooltip("Define properties for loading target scenes from this scene.")]
        private SceneLoader sceneLoader = new SceneLoader();

        [SerializeField]
        private int targetFrameRate = 60;

        private void Awake()
        {
            bool isWebGL = false;
#if UNITY_WEBGL
            enableMultiplayer = false;
            isWebGL = true;
#endif

            if(multiplayerButton)
                multiplayerButton.SetActive(enableMultiplayer);
            if(webGLMultiplayerMsg)
                webGLMultiplayerMsg.SetActive(!enableMultiplayer);

            if(!isWebGL)
                exitButton.SetActive(true);

            Application.targetFrameRate = targetFrameRate;
        }

        public void LeaveGame ()
		{
			Application.Quit ();
		}

		public void LoadScene(string sceneName)
		{
            sceneLoader.LoadScene(sceneName, source: this);
		}
	}
}