using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Labyrinth.Editor
{
    // Ejecuta pruebas con el código real fuera del modo Play.
    // Ejecuta pruebas del balance y las recompensas.
    public static class BalanceChecks
    {
        static int assertions;

        // Lanza las pruebas desde el menú.
        static void Check(bool ok, string text)
        {
            if (!ok)throw new Exception("FAIL: "+text);
            assertions++;
        }
        static object Invoke(object target, string name, params object[] args)
        {
            return target.GetType().GetMethod(name, BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target, args);
        }
        static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target, value);
        }
        static T Child<T>(GameObject root, string name) where T:Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(root.transform);
            return obj.AddComponent<T>();
        }
        [MenuItem("HEXENKNALL/Run balance and layout checks")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Deten Play primero.");
                return;
            }
            var previous = GameDirector.I;
            var random = UnityEngine.Random.state;
            float scale = Time.timeScale;
            var scene = EditorSceneManager.NewPreviewScene();
            assertions = 0;
            try
            {
                var root = new GameObject("Balance checks");
                SceneManager.MoveGameObjectToScene(root, scene);
                var board = root.AddComponent<GridBoard>();
                board.width = 9;
                board.height = 7;
                var director = root.AddComponent<GameDirector>();
                director.board = board;
                director.stage = 1;
                GameDirector.I = director;
                Time.timeScale = 1;
                var actor = Child<GridActor>(root, "Actor");
                actor.cell = new Vector2Int(1, 1);
                actor.transform.position = board.World(actor.cell);
                actor.speed = 4;
                board.actors.Add(actor);
                Check(actor.Step(Vector2Int.right), "A normal step starts");
                Check(actor.Covers(new Vector2Int(2, 1)), "Destination is reserved");
                Check(!actor.OverlapsDamageCell(new Vector2Int(2, 1)), "Reservation is not a hitbox");
                actor.AdvanceMovement(.025f);
                Check(!actor.OverlapsDamageCell(new Vector2Int(2, 1)), "No early hit at 10% of step");
                actor.AdvanceMovement(.15f);
                Check(!actor.OverlapsDamageCell(new Vector2Int(1, 1)), "No stale source hit at 70% of step");
                Check(actor.OverlapsDamageCell(new Vector2Int(2, 1)), "Real overlap can take damage");
                Check(actor.OverlapsDamageCell(new Vector2Int(1, 1), .44f, true), "Existing flame sees movement crossing it");
                actor.AdvanceMovement(.075f);
                actor.health = 10;
                actor.invulnerability = 0;
                var shared = new HashSet<GridActor>();
                var h1 = Child<Hazard>(root, "Flame 1");
                h1.cell = actor.cell;
                h1.bombDamage = true;
                h1.hitActors = shared;
                var h2 = Child<Hazard>(root, "Flame 2");
                h2.cell = actor.cell;
                h2.bombDamage = true;
                h2.hitActors = shared;
                Set(h1, "board", board);
                Set(h2, "board", board);
                Set(h1, "bornFrame", Time.frameCount);
                Set(h2, "bornFrame", Time.frameCount);
                Invoke(h1, "LateUpdate");
                Invoke(h2, "LateUpdate");
                Check(actor.health == 9, "One explosion hits once even with zero immunity");
                board.hazards.Clear();
                board.actors.Clear();
                var enemy = Child<EnemyController>(root, "Enemy");
                enemy.cell = new Vector2Int(2, 2);
                enemy.transform.position = board.World(enemy.cell);
                enemy.speed = 2.4f;
                board.actors.Add(enemy);
                actor.cell = new Vector2Int(3, 2);
                actor.transform.position = board.World(new Vector2Int(4, 2));
                Check(!enemy.CanMelee(actor), "Melee uses real distance, not stale grid cell");
                actor.transform.position = board.World(actor.cell);
                Check(enemy.CanMelee(actor), "Adjacent real target in reach");
                actor.transform.position = board.World(new Vector2Int(3, 3));
                Check(!enemy.CanMelee(actor), "No melee around a diagonal corner");
                Invoke(enemy, "BuildForecast", board);
                Invoke(enemy, "AddBlast", board.Blast(enemy.cell, 2), 2f, .38f);
                var step = (Vector2Int)Invoke(enemy, "FindStep", new Vector2Int(6, 2), true);
                Check((step-enemy.cell).sqrMagnitude == 1, "Own bomb has an immediate cardinal escape step");
                var bomb = Child<Bomb>(root, "Observed bomb");
                bomb.cell = new Vector2Int(3, 2);
                bomb.fuse = 2;
                Set(bomb, "placedAt", Time.time);
                board.bombs[bomb.cell] = bomb;
                Invoke(enemy, "ObserveBombs", board);
                Invoke(enemy, "BuildForecast", board);
                Check(!(bool)Invoke(enemy, "UnsafeDuring", enemy.cell, 0f, 3f), "Opponent bomb is not known instantly");
                var known = (Dictionary<Bomb, float>)typeof(EnemyController).GetField("noticed", BindingFlags.Instance|BindingFlags.NonPublic).GetValue(enemy);
                known[bomb] = Time.time-1;
                bomb.fuse = .2f;
                var chain = Child<Bomb>(root, "Chain bomb");
                chain.cell = new Vector2Int(5, 2);
                chain.fuse = 5;
                Set(chain, "placedAt", Time.time);
                board.bombs[chain.cell] = chain;
                known[chain] = Time.time-1;
                Invoke(enemy, "BuildForecast", board);
                Check((bool)Invoke(enemy, "UnsafeDuring", new Vector2Int(6, 2), 0f, .5f), "Chain fuse is forecast early");
                CheckCards();
                CheckLayouts();
                Debug.Log("PASS: "+assertions+" assertions (damage, AI, cards, layouts). Test visuals and feel in Play Mode too.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                GameDirector.I = previous;
                UnityEngine.Random.state = random;
                Time.timeScale = scale;
            }
        }

        // Comprueba las reglas de las cartas.
        static void CheckCards()
        {
            var old = new RunData
            {
                stage = 5, rewardStage = 4, rewardPhase = 2, chosenRarity = 2, bombDamageBonus = 4, rayDamageBonus = 3, enemyDamageBonus = 2
            };
            old.offers.Add(new CardOffer(CardEffect.EnemyDamage, Rarity.Epic, true));
            old.offers.Add(new CardOffer(CardEffect.EnemyHealth, Rarity.Epic, true));
            old.offers.Add(new CardOffer(CardEffect.EnemySpeed, Rarity.Epic, true));
            RunProgress.RemoveDamageModifiers(old);
            Check(old.stage == 5 && old.rewardPhase == 2, "Migration preserves campaign and pending curse");
            Check(old.bombDamageBonus == 0 && old.rayDamageBonus == 0 && old.enemyDamageBonus == 0, "Old damage bonuses removed");
            var ids = new HashSet<CardEffect>();
            foreach (var offer in old.offers)
            {
                Check(!CardRules.Removed(offer.effect) && offer.rarity == Rarity.Epic, "Pending curse replaced at same rarity");
                ids.Add(offer.effect);
            }
            Check(ids.Count == 3, "Migration preserves three different choices");
            for (int run = 0;run<100;run++)
            {
                var data = new RunData();
                for (int level = 1;level<=9;level++)
                {
                    var boons = CardRules.Roll(data, false);
                    Check(boons.Count == 3, "Three boons throughout campaign");
                    foreach (var card in boons)Check(!CardRules.Removed(card.effect), "No damage boon offered");
                    var chosen = boons[UnityEngine.Random.Range(0, 3)];
                    CardRules.Apply(chosen, data);
                    var curses = CardRules.Roll(data, true, chosen.rarity);
                    Check(curses.Count == 3, "Three curses throughout campaign");
                    foreach (var card in curses)Check(!CardRules.Removed(card.effect) && card.rarity == chosen.rarity, "No damage curse; matching rarity");
                    CardRules.Apply(curses[UnityEngine.Random.Range(0, 3)], data);
                    Check(data.bombDamageBonus == 0 && data.rayDamageBonus == 0 && data.enemyDamageBonus == 0, "No hidden damage modifiers");
                }
            }
        }

        // Comprueba la distribución del tablero.
        static void CheckLayouts()
        {
            var protectedCells = new HashSet<Vector2Int>();
            ProceduralLayout.Protect(protectedCells, new Vector2Int(1, 1), 2);
            ProceduralLayout.Protect(protectedCells, new Vector2Int(11, 9), 2);
            var variants = new HashSet<string>();
            for (int seed = 0;seed<24;seed++)
            {
                var cells = ProceduralLayout.Interior(13, 11);
                var plan = ProceduralLayout.Build(13, 11, cells, new HashSet<Vector2Int>(), protectedCells, .14f, .22f, seed);
                Check(!plan.walls.Overlaps(protectedCells) && !plan.crates.Overlaps(protectedCells), "Free spawn area");
                Check(!plan.walls.Overlaps(plan.crates), "No stacked wall/crate");
                var floor = new HashSet<Vector2Int>(cells);
                floor.ExceptWith(plan.walls);
                floor.ExceptWith(plan.crates);
                Check(Connected(floor).Count == floor.Count, "Initial floor remains connected");
                var fingerprint = new List<string>();
                foreach (var p in plan.walls)fingerprint.Add(p.x+","+p.y);
                fingerprint.Sort();
                variants.Add(string.Join(";", fingerprint));
                var again = ProceduralLayout.Build(13, 11, cells, new HashSet<Vector2Int>(), protectedCells, .14f, .22f, seed);
                Check(again.walls.SetEquals(plan.walls) && again.crates.SetEquals(plan.crates), "Fixed seed is reproducible");
                var added = new List<Vector2Int>();
                foreach (var p in ProceduralLayout.Interior(21, 19))if(p.x>=13 || p.y>=11)added.Add(p);
                var fixedBlocks = new HashSet<Vector2Int>(plan.walls);
                fixedBlocks.UnionWith(plan.crates);
                var expanded = ProceduralLayout.Build(21, 19, added, fixedBlocks, protectedCells, .14f, .22f, seed+1000);
                foreach (var p in expanded.walls)Check(p.x>=13 || p.y>=11, "Expansion preserves old layout");
                foreach (var p in expanded.crates)Check(p.x>=13 || p.y>=11, "Expansion crates only on new terrain");
                floor = new HashSet<Vector2Int>(ProceduralLayout.Interior(21, 19));
                floor.ExceptWith(fixedBlocks);
                floor.ExceptWith(expanded.walls);
                floor.ExceptWith(expanded.crates);
                Check(Connected(floor).Count == floor.Count, "Expanded floor remains connected");
            }
            Check(variants.Count>=20, "Different seeds produce different wall layouts");
        }
        static HashSet<Vector2Int> Connected(HashSet<Vector2Int> floor)
        {
            var seen = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            foreach (var p in floor)
            {
                seen.Add(p);
                queue.Enqueue(p);
                break;
            }
            while (queue.Count>0)
            {
                var p = queue.Dequeue();
                foreach (var d in GridBoard.Directions)
                {
                    var n = p+d;
                    if (floor.Contains(n) && seen.Add(n))queue.Enqueue(n);
                }
            }
            return seen;
        }
    }
}
