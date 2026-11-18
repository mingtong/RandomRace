using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RandomRace.Editor
{
    public static class CreateRaceScene
    {
        [MenuItem("Random Race/Create Prototype Scene")]
        public static void Create()
        {
            const string path = "Assets/RandomRacePrototype/RandomRace.unity";
            if (System.IO.File.Exists(path) && !EditorUtility.DisplayDialog("Random Race", "The prototype scene already exists. Replace it?", "Replace", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Random Race").AddComponent<RaceView>();
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("Random Race scene created: " + path + ". Press Play to start.");
        }
    }
}
