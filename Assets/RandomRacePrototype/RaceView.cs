using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RandomRace
{
    public sealed class RaceView : MonoBehaviour
    {
        readonly List<Material> generatedMaterials = new List<Material>();
        RaceGame game; GameObject[] pieces = new GameObject[4]; Camera boardCamera; readonly Renderer[,] tiles = new Renderer[RaceGame.Width, RaceGame.Height]; readonly Color[,] tileColors = new Color[RaceGame.Width, RaceGame.Height]; string message = "点击道路移动，完成后结束回合。";
        Font uiFont; GUIStyle labelStyle; GUIStyle headingStyle; bool confirmRestart; bool animating; bool botsRunning; bool hasHover; Cell hover; string preview = "将鼠标移到道路上，预览路线。";
        readonly Color[] colors = { new Color(.15f,.55f,1), new Color(1,.3f,.25f), new Color(1,.8f,.15f), new Color(.65f,.3f,.9f) };
        void Start()
        {
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "PingFang SC", "Microsoft YaHei", "Noto Sans CJK SC", "Arial Unicode MS" }, 18);
            NewGame();
        }
        float UIScale { get { return Mathf.Clamp(Mathf.Min(Screen.height / 660f, Screen.width / 900f), .45f, 1.25f); } }
        void UpdateCameraLayout()
        {
            float left = Mathf.Min(.48f, (12f + 290f * UIScale) / Mathf.Max(1f, Screen.width));
            boardCamera.rect = new Rect(left, 0, 1f - left, 1f);
            float aspect = Mathf.Max(.1f, (Screen.width * (1f - left)) / Mathf.Max(1f, Screen.height));
            boardCamera.orthographicSize = Mathf.Max(6.2f, 5.9f / aspect);
        }
        string PlayerName(int index) { return index == 0 ? "玩家" : "机器人 " + index; }
        void NewGame()
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
            ClearMaterials();
            game = new RaceGame(Environment.TickCount); StopAllCoroutines(); animating = false; botsRunning = false; confirmRestart = false; hasHover = false; preview = "将鼠标移到道路上，预览路线。";
            boardCamera = Camera.main;
            if (boardCamera == null) { var c = new GameObject("Race Camera"); boardCamera = c.AddComponent<Camera>(); c.tag = "MainCamera"; }
            boardCamera.transform.position = new Vector3(4.5f, 12f, -8f); boardCamera.transform.LookAt(new Vector3(4.5f,0,3.5f)); boardCamera.orthographic = true; boardCamera.orthographicSize = 7.5f; boardCamera.clearFlags = CameraClearFlags.SolidColor; boardCamera.backgroundColor = new Color(.12f, .19f, .21f);
            UpdateCameraLayout();
            var lightObj = new GameObject("Race Light"); lightObj.transform.SetParent(transform); var light = lightObj.AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(50,-30,0);
            // Layered wooden frame gives the generated grid the feel of a physical board.
            Shape("Board base", PrimitiveType.Cube, transform, new Vector3(4.5f,-.34f,3.5f), new Vector3(10.8f,.5f,8.8f), new Color(.28f,.15f,.09f));
            Shape("Board inset", PrimitiveType.Cube, transform, new Vector3(4.5f,-.07f,3.5f), new Vector3(10.4f,.12f,8.4f), new Color(.52f,.32f,.16f));
            for (int x=0;x<RaceGame.Width;x++) for(int y=0;y<RaceGame.Height;y++)
            {
                var terrain=game.Map[x,y]; var tile=GameObject.CreatePrimitive(PrimitiveType.Cube); tile.name="Tile " + x + "," + y; tile.transform.SetParent(transform); tile.transform.position=new Vector3(x,0,y); tile.transform.localScale=new Vector3(.92f,.16f,.92f);
                float variation = ((x * 17 + y * 31) % 5) * .018f;
                var color=terrain==Terrain.Water ? new Color(.12f,.48f,.61f) : terrain==Terrain.Obstacle ? new Color(.44f,.49f,.35f) : new Color(.61f+variation,.68f+variation,.40f+variation);
                if (new Cell(x,y).Equals(game.Finish)) color = new Color(1,.86f,.2f); else if(x==0 && y==RaceGame.Height/2) color = new Color(.2f,.8f,.65f);
                SetColor(tile,color); tiles[x,y] = tile.GetComponent<Renderer>(); tileColors[x,y] = color;
                if (terrain == Terrain.Water)
                {
                    for (int wave = 0; wave < 2; wave++)
                        Shape("Water glint", PrimitiveType.Cube, transform, new Vector3(x + (wave == 0 ? -.15f : .13f), .09f, y + (wave == 0 ? -.15f : .15f)), new Vector3(.29f,.025f,.035f), new Color(.47f,.78f,.79f));
                }
                else if (terrain == Terrain.Obstacle)
                {
                    var rock = Shape("Boulder", PrimitiveType.Sphere, transform, new Vector3(x,.22f,y), new Vector3(.66f,.43f,.63f), new Color(.38f,.36f,.31f));
                    rock.transform.rotation = Quaternion.Euler(0, (x * 43 + y * 19) % 90, 0);
                    Shape("Small stone", PrimitiveType.Sphere, transform, new Vector3(x+.26f,.15f,y-.22f), new Vector3(.24f,.18f,.22f), new Color(.55f,.51f,.41f));
                }
                if (new Cell(x,y).Equals(game.Finish))
                    for (int a=0; a<2; a++) for (int b=0; b<2; b++)
                        Shape("Finish checker", PrimitiveType.Cube, transform, new Vector3(x+(a-.5f)*.29f,.09f,y+(b-.5f)*.29f), new Vector3(.27f,.025f,.27f), (a+b)%2==0 ? new Color(.95f,.91f,.75f) : new Color(.24f,.22f,.18f));
            }
            for(int i=0;i<4;i++)
            {
                pieces[i]=new GameObject(game.Players[i].Name); pieces[i].transform.SetParent(transform);
                Shape("Pawn base", PrimitiveType.Cylinder, pieces[i].transform, Vector3.zero, new Vector3(.30f,.045f,.30f), colors[i]);
                Shape("Pawn body", PrimitiveType.Cylinder, pieces[i].transform, new Vector3(0,.13f,0), new Vector3(.19f,.11f,.19f), colors[i]);
                Shape("Pawn head", PrimitiveType.Sphere, pieces[i].transform, new Vector3(0,.30f,0), new Vector3(.24f,.23f,.22f), new Color(.96f,.77f,.58f));
                Shape("Cap brim", PrimitiveType.Cylinder, pieces[i].transform, new Vector3(0,.40f,0), new Vector3(.26f,.025f,.25f), colors[i]);
                Shape("Cap", PrimitiveType.Sphere, pieces[i].transform, new Vector3(0,.43f,0), new Vector3(.21f,.12f,.20f), colors[i]);
                Shape("Left eye", PrimitiveType.Sphere, pieces[i].transform, new Vector3(-.047f,.31f,-.105f), Vector3.one*.035f, new Color(.12f,.10f,.09f));
                Shape("Right eye", PrimitiveType.Sphere, pieces[i].transform, new Vector3(.047f,.31f,-.105f), Vector3.one*.035f, new Color(.12f,.10f,.09f));
            }
            Refresh(); message="点击道路移动，完成后结束回合。";
        }
        GameObject Shape(string label, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = label; obj.transform.SetParent(parent);
            obj.transform.localPosition = position; obj.transform.localScale = scale; SetColor(obj, color); return obj;
        }
        void SetColor(GameObject obj, Color color) { var shader=Shader.Find("Universal Render Pipeline/Lit"); if(shader==null) shader=Shader.Find("Standard"); var mat=new Material(shader); generatedMaterials.Add(mat); mat.color=color; obj.GetComponent<Renderer>().material=mat; }
        void ClearMaterials() { foreach (var material in generatedMaterials) if (material != null) Destroy(material); generatedMaterials.Clear(); }
        void OnDestroy() { ClearMaterials(); }
        void Refresh()
        {
            for(int i=0;i<4;i++) { var p=game.Players[i].Position; pieces[i].transform.position=new Vector3(p.X+(i%2==0?-.2f:.2f),.13f,p.Y+(i<2?-.2f:.2f)); }
            var player = game.Players[0];
            List<Cell> hoverPath = null; int hoverCost = 0;
            preview = "将鼠标移到道路上，预览路线。";
            if (hasHover && game.Winner < 0 && game.Turn == 0)
            {
                hoverPath = game.Path(player.Position, hover);
                if (hoverPath == null) preview = "此格无法到达。";
                else if (hoverPath.Count == 0) { preview = "这是当前位置。"; hoverPath = null; }
                else
                {
                    int finish = hoverPath.FindIndex(c => c.Equals(game.Finish));
                    if (finish >= 0 && finish + 1 < hoverPath.Count) hoverPath.RemoveRange(finish + 1, hoverPath.Count - finish - 1);
                    hoverCost = hoverPath.Count;
                    preview = "路线消耗 " + hoverCost + " 点体力" + (hoverCost > player.Stamina ? "（体力不足）" : "，点击移动");
                }
            }
            for (int x=0; x<RaceGame.Width; x++) for (int y=0; y<RaceGame.Height; y++)
            {
                var cell = new Cell(x,y); var color = tileColors[x,y];
                if (game.Winner < 0 && game.Turn == 0 && !cell.Equals(player.Position) && !cell.Equals(game.Finish) && game.Walkable(cell))
                {
                    var path = game.Path(player.Position, cell);
                    int finishIndex = path == null ? -1 : path.FindIndex(c => c.Equals(game.Finish));
                    int cost = path == null ? int.MaxValue : finishIndex >= 0 ? finishIndex + 1 : path.Count;
                    if (cost <= player.Stamina) color = Color.Lerp(color, new Color(.2f, 1f, .9f), .55f);
                }
                if (hoverPath != null && hoverPath.Contains(cell) && !cell.Equals(game.Finish))
                    color = hoverCost <= player.Stamina ? new Color(1f, .68f, .2f) : new Color(.9f, .28f, .28f);
                tiles[x,y].material.color = color;
            }
        }
        void Update()
        {
            if (game == null) return;
            UpdateCameraLayout();
            if (animating || botsRunning || game.Winner >= 0 || game.Turn != 0 || Mouse.current == null) return;
            var mouse = Mouse.current.position.ReadValue();
            float scale = UIScale; bool overPanel = mouse.x >= 12 * scale && mouse.x <= 302 * scale && Screen.height - mouse.y >= 12 * scale && Screen.height - mouse.y <= 632 * scale;
            bool found = false; Cell cell = default(Cell);
            if (!overPanel)
            {
                var ray = boardCamera.ScreenPointToRay(mouse); float distance;
                if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out distance))
                {
                    var pos = ray.GetPoint(distance);
                    int x = Mathf.RoundToInt(pos.x), y = Mathf.RoundToInt(pos.z);
                    if (x >= 0 && x < RaceGame.Width && y >= 0 && y < RaceGame.Height) { found = true; cell = new Cell(x, y); }
                }
            }
            if (found != hasHover || (found && !cell.Equals(hover))) { hasHover = found; hover = cell; Refresh(); }
            if (found && Mouse.current.leftButton.wasPressedThisFrame)
            {
                var start = game.Players[0].Position; var path = game.Path(start, cell);
                if (game.Move(cell))
                {
                    int actualSteps = path.FindIndex(c => c.Equals(game.Players[0].Position)) + 1;
                    if (actualSteps < path.Count) path.RemoveRange(actualSteps, path.Count - actualSteps);
                    message = "移动完成，可以继续移动或结束回合。"; StartCoroutine(AnimateMove(0, start, path));
                }
                else message = "请选择体力范围内可到达的道路。";
            }
        }
        Vector3 PiecePosition(int index, Cell cell)
        {
            return new Vector3(cell.X + (index % 2 == 0 ? -.2f : .2f), .13f, cell.Y + (index < 2 ? -.2f : .2f));
        }
        IEnumerator AnimateMove(int index, Cell start, List<Cell> path)
        {
            animating = true; var piece = pieces[index].transform; Vector3 from = PiecePosition(index, start);
            foreach (Cell cell in path)
            {
                Vector3 to = PiecePosition(index, cell); float elapsed = 0f; const float duration = .22f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime; float t = Mathf.Clamp01(elapsed / duration);
                    piece.position = Vector3.Lerp(from, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .13f);
                    piece.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 2f) * 10f);
                    yield return null;
                }
                from = to;
            }
            piece.position = from; piece.localRotation = Quaternion.identity; animating = false; Refresh();
        }
        IEnumerator Bots()
        {
            while(game.Winner<0 && game.Turn!=0)
            {
                int bot = game.Turn; var racer = game.Players[bot]; var before = racer.Position;
                message = PlayerName(bot) + " 正在行动……";
                yield return new WaitForSeconds(.6f);
                game.BotMove();
                var details = "";
                if (game.LastBotEnergyCards > 0) details += "使用能量卡 " + game.LastBotEnergyCards + " 张；";
                if (game.LastBotUsedSkill) details += "使用体力技能；";
                if (game.LastBotSlowTarget >= 0) details += "减速 " + PlayerName(game.LastBotSlowTarget) + "（-" + game.LastBotSlowAmount + " 体力）；";
                if (details.Length > 0) { message = PlayerName(bot) + "：" + details; yield return new WaitForSeconds(1.1f); }
                var route = game.Path(before, racer.Position);
                if (route != null && route.Count > 0) yield return AnimateMove(bot, before, route);
                else Refresh();
                message = PlayerName(bot) + " 移动了 " + (route == null ? 0 : route.Count) + " 格。";
                yield return new WaitForSeconds(.6f);
            }
            botsRunning = false;
            if(game.Winner<0) { message="轮到你了：青色道路可以到达。"; Refresh(); }
        }
        void OnGUI()
        {
            if (game == null) return;
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 16, font = uiFont };
                headingStyle = new GUIStyle(labelStyle) { fontSize = 20, fontStyle = FontStyle.Bold };
            }
            var oldMatrix = GUI.matrix; var oldFont = GUI.skin.font;
            GUI.skin.font = uiFont; GUI.matrix = Matrix4x4.Scale(Vector3.one * UIScale);
            GUILayout.BeginArea(new Rect(12, 12, 290, 620), GUI.skin.box);
            GUILayout.Label("随机竞速 · 试玩版", headingStyle);
            GUILayout.Label(game.Winner >= 0 ? "胜利！ " + PlayerName(game.Winner) + " 获胜！" : "当前回合：" + PlayerName(game.Turn), headingStyle);
            for (int i = 0; i < 4; i++)
            {
                GUI.contentColor = colors[i]; GUILayout.Label(PlayerName(i) + "  体力 " + game.Players[i].Stamina + "/8", labelStyle);
            }
            GUI.contentColor = Color.white;
            GUILayout.Space(4); GUILayout.Label(message, labelStyle);
            if (game.Winner < 0 && game.Turn == 0) GUILayout.Label(preview, labelStyle);
            GUILayout.Label("青色：可达  金色：终点\n橙色：路线  红色：体力不足\n水域和岩石无法通行", labelStyle);
            if (game.Winner < 0 && game.Turn == 0)
            {
                var player = game.Players[0];
                GUI.enabled = !animating && !botsRunning && !player.SkillUsed && player.Stamina < RaceGame.MaxStamina;
                if (GUILayout.Button(player.SkillUsed ? "技能：本局已使用" : "技能：恢复 2 点体力（每局一次）", GUILayout.Height(30)))
                { int before = player.Stamina; game.UseSkill(); message = "技能恢复 " + (player.Stamina - before) + " 点体力。"; Refresh(); }
                GUI.enabled = !animating && !botsRunning;
                for (int i = 0; i < player.Hand.Count; i++)
                {
                    Card card = player.Hand[i]; string label = card == Card.Energy ? "能量卡：恢复 2 点体力" : "减速卡：领先对手减少 2 点体力";
                    if (GUILayout.Button(label, GUILayout.Height(30)))
                    {
                        var target = card == Card.Slow ? game.LeadingOpponent(player) : player; int before = target.Stamina;
                        if (game.UseCard(i))
                        {
                            int delta = Math.Abs(target.Stamina - before);
                            message = card == Card.Energy ? "能量卡恢复 " + delta + " 点体力。" : "减速卡令 " + PlayerName(game.Players.IndexOf(target)) + " 减少 " + delta + " 点体力。";
                            Refresh();
                        }
                        else message = "当前不能使用这张卡。";
                        break;
                    }
                }
                GUI.enabled = true; GUILayout.Label("每回合抽 1 张，手牌上限 3 张。", labelStyle);
            }
            GUI.enabled = !animating && !botsRunning;
            if (game.Winner < 0 && game.Turn == 0 && GUILayout.Button("结束回合", GUILayout.Height(34))) { game.EndTurn(); botsRunning = true; Refresh(); StartCoroutine(Bots()); }
            if (game.Winner >= 0)
            {
                if (GUILayout.Button("再来一局", GUILayout.Height(34))) NewGame();
            }
            else if (!confirmRestart)
            {
                if (GUILayout.Button("重新开始", GUILayout.Height(34))) confirmRestart = true;
            }
            else
            {
                GUILayout.Label("确定放弃当前对局？", labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("确定", GUILayout.Height(30))) NewGame();
                if (GUILayout.Button("取消", GUILayout.Height(30))) confirmRestart = false;
                GUILayout.EndHorizontal();
            }
            GUI.enabled = true; GUILayout.EndArea(); GUI.matrix = oldMatrix; GUI.skin.font = oldFont;
        }
    }
}
