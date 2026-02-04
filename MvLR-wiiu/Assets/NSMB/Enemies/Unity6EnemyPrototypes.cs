using UnityEngine;

namespace NSMB.Enemies {
    internal static class Unity6EnemyPrototypes {
        internal const float Unity6ToUnity2017WorldScale = 2f;
        private const float RawDiv = 65536f;

        private static Sprite[] _goombaWalkFrames;
        private static Sprite[] _koopaWalkFrames;
        private static Sprite[] _koopaShellFrames;
        private static Sprite[] _booIdleFrames;
        private static Sprite[] _bobombWalkFrames;
        private static Sprite[] _piranhaFrames;

        internal static void ApplyGoomba(GameObject root, out Transform graphics, out SpriteRenderer sr, out NSMB.Visual.SimpleSpriteAnimator anim) {
            ApplyBoxCollider2D(root, 9830, 13107, 0, 13173);
            EnsureGraphics(root, Vector3.zero, new Vector3(1f, 1f, 1f), out graphics, out sr, out anim);

            Sprite[] frames = GetGoombaWalkFrames();
            if (frames != null && frames.Length > 0) {
                sr.sprite = frames[0];
                if (anim == null) {
                    anim = graphics.gameObject.AddComponent<NSMB.Visual.SimpleSpriteAnimator>();
                }
                anim.SetFrames(frames, 12f, true);
            }
        }

        internal static void ApplyKoopaGreen(GameObject root, out Transform graphics, out SpriteRenderer sr, out NSMB.Visual.SimpleSpriteAnimator anim) {
            ApplyBoxCollider2D(root, 9830, 14746, 0, 14811);
            EnsureGraphics(root, new Vector3(0f, 0.235f, 0f), new Vector3(1f, 1f, 1f), out graphics, out sr, out anim);

            Sprite[] frames = GetKoopaGreenWalkFrames();
            if (frames != null && frames.Length > 0) {
                sr.sprite = frames[0];
                if (anim == null) {
                    anim = graphics.gameObject.AddComponent<NSMB.Visual.SimpleSpriteAnimator>();
                }
                // Unity 6 controller plays the walk clip at 0.33333334 speed (60 * 1/3 = 20 fps effective).
                anim.SetFrames(frames, 20f, true);
            }
        }

        internal static void ApplyBulletBill(GameObject root, out Transform graphics, out SpriteRenderer sr, out NSMB.Visual.SimpleSpriteAnimator anim) {
            ApplyBoxCollider2D(root, 16711, 9830, 0, 9830);
            EnsureGraphics(root, Vector3.zero, new Vector3(3f, 3f, 1f), out graphics, out sr, out anim);

            Sprite sprite = NSMB.Content.ResourceSpriteCache.FindSprite(NSMB.Content.GameplayAtlasPaths.BulletBill, "bullet-bill_0");
            if (sprite != null) {
                sr.sprite = sprite;
            }
        }

        internal static void ApplyBoo(GameObject root, out Transform graphics, out SpriteRenderer sr, out NSMB.Visual.SimpleSpriteAnimator anim) {
            ApplyBoxCollider2D(root, 16384, 16384, 0, 0);
            EnsureGraphics(root, Vector3.zero, new Vector3(1f, 1f, 1f), out graphics, out sr, out anim);

            Sprite[] frames = GetBooIdleFrames();
            if (frames != null && frames.Length > 0) {
                sr.sprite = frames[0];
                if (anim == null) {
                    anim = graphics.gameObject.AddComponent<NSMB.Visual.SimpleSpriteAnimator>();
                }
                anim.SetFrames(frames, 60f, true);
            }
        }

        internal static void ApplyBobomb(GameObject root, out Transform graphics, out SpriteRenderer sr, out NSMB.Visual.SimpleSpriteAnimator anim) {
            ApplyBoxCollider2D(root, 10321, 10321, 0, 12288);
            EnsureGraphics(root, Vector3.zero, new Vector3(1f, 1f, 1f), out graphics, out sr, out anim);

            Sprite[] frames = GetBobombWalkFrames();
            if (frames != null && frames.Length > 0) {
                sr.sprite = frames[0];
                if (anim == null) {
                    anim = graphics.gameObject.AddComponent<NSMB.Visual.SimpleSpriteAnimator>();
                }
                anim.SetFrames(frames, 60f, true);
            }
        }

        internal static void ApplyPiranhaPlant(GameObject root, out Transform graphics, out SpriteRenderer sr, out NSMB.Visual.SimpleSpriteAnimator anim) {
            ApplyBoxCollider2D(root, 14746, 24576, 0, 24576);
            EnsureGraphics(root, Vector3.zero, new Vector3(1f, 1f, 1f), out graphics, out sr, out anim);

            Sprite[] frames = GetPiranhaPlantFrames();
            if (frames != null && frames.Length > 0) {
                sr.sprite = frames[0];
                if (anim == null) {
                    anim = graphics.gameObject.AddComponent<NSMB.Visual.SimpleSpriteAnimator>();
                }
                anim.SetFrames(frames, 60f, true);
            }
        }

        internal static Sprite[] GetGoombaWalkFrames() {
            if (_goombaWalkFrames != null) {
                return _goombaWalkFrames;
            }

            string[] names = new string[] {
                "goomba_0",
                "goomba_1",
                "goomba_2",
                "goomba_3",
                "goomba_4",
                "goomba_5",
                "goomba_6",
                "goomba_7",
            };
            _goombaWalkFrames = LoadSprites(NSMB.Content.GameplayAtlasPaths.Goomba, names);
            return _goombaWalkFrames;
        }

        internal static Sprite[] GetKoopaGreenWalkFrames() {
            if (_koopaWalkFrames != null) {
                return _koopaWalkFrames;
            }

            string[] names = new string[] {
                "koopa_13",
                "koopa_14",
                "koopa_15",
                "koopa_0",
                "koopa_1",
                "koopa_2",
                "koopa_3",
                "koopa_4",
                "koopa_5",
                "koopa_6",
                "koopa_7",
                "koopa_8",
                "koopa_9",
                "koopa_10",
                "koopa_11",
                "koopa_12",
            };
            _koopaWalkFrames = LoadSprites(NSMB.Content.GameplayAtlasPaths.Koopa, names);
            return _koopaWalkFrames;
        }

        internal static Sprite[] GetKoopaGreenShellFrames() {
            if (_koopaShellFrames != null) {
                return _koopaShellFrames;
            }

            string[] names = new string[] {
                "koopa_19",
                "koopa_20",
                "koopa_21",
                "koopa_20",
            };
            _koopaShellFrames = LoadSprites(NSMB.Content.GameplayAtlasPaths.Koopa, names);
            return _koopaShellFrames;
        }

        private static Sprite[] GetBooIdleFrames() {
            if (_booIdleFrames != null) {
                return _booIdleFrames;
            }
            _booIdleFrames = NSMB.Content.ResourceSpriteCache.FindSpritesByPrefix(NSMB.Content.GameplayAtlasPaths.Boo, "boo_");
            return _booIdleFrames;
        }

        private static Sprite[] GetBobombWalkFrames() {
            if (_bobombWalkFrames != null) {
                return _bobombWalkFrames;
            }
            _bobombWalkFrames = NSMB.Content.ResourceSpriteCache.FindSpritesByPrefix(NSMB.Content.GameplayAtlasPaths.Bobomb, "bobomb_");
            return _bobombWalkFrames;
        }

        private static Sprite[] GetPiranhaPlantFrames() {
            if (_piranhaFrames != null) {
                return _piranhaFrames;
            }
            _piranhaFrames = NSMB.Content.ResourceSpriteCache.FindSpritesByPrefix(NSMB.Content.GameplayAtlasPaths.PiranhaPlant, "piranhaplant_");
            return _piranhaFrames;
        }

        private static Sprite[] LoadSprites(string atlasPath, string[] names) {
            if (string.IsNullOrEmpty(atlasPath) || names == null || names.Length == 0) {
                return new Sprite[0];
            }

            Sprite[] result = new Sprite[names.Length];
            int count = 0;
            for (int i = 0; i < names.Length; i++) {
                string n = names[i];
                if (string.IsNullOrEmpty(n)) {
                    continue;
                }

                Sprite s = NSMB.Content.ResourceSpriteCache.FindSprite(atlasPath, n);
                if (s != null) {
                    result[count++] = s;
                }
            }

            if (count == names.Length) {
                return result;
            }

            Sprite[] trimmed = new Sprite[count];
            for (int i = 0; i < count; i++) {
                trimmed[i] = result[i];
            }
            return trimmed;
        }

        private static void EnsureGraphics(GameObject root, Vector3 unity6LocalPos, Vector3 unity6LocalScale, out Transform graphics, out SpriteRenderer sr, out NSMB.Visual.SimpleSpriteAnimator anim) {
            graphics = null;
            sr = null;
            anim = null;

            if (root == null) {
                return;
            }

            Transform existing = root.transform.Find("Graphics");
            if (existing == null) {
                GameObject g = new GameObject("Graphics");
                existing = g.transform;
                existing.parent = root.transform;
                existing.localRotation = Quaternion.identity;
            }

            existing.localPosition = unity6LocalPos * Unity6ToUnity2017WorldScale;
            existing.localScale = new Vector3(
                unity6LocalScale.x * Unity6ToUnity2017WorldScale,
                unity6LocalScale.y * Unity6ToUnity2017WorldScale,
                unity6LocalScale.z
            );

            SpriteRenderer rootSr = root.GetComponent<SpriteRenderer>();
            if (rootSr != null) {
                Object.Destroy(rootSr);
            }

            NSMB.Visual.SimpleSpriteAnimator rootAnim = root.GetComponent<NSMB.Visual.SimpleSpriteAnimator>();
            if (rootAnim != null) {
                Object.Destroy(rootAnim);
            }

            sr = existing.GetComponent<SpriteRenderer>();
            if (sr == null) {
                sr = existing.gameObject.AddComponent<SpriteRenderer>();
            }
            sr.color = Color.white;
            sr.sortingOrder = 0;

            anim = existing.GetComponent<NSMB.Visual.SimpleSpriteAnimator>();
            graphics = existing;
        }

        private static void ApplyBoxCollider2D(GameObject root, int extXRaw, int extYRaw, int offXRaw, int offYRaw) {
            if (root == null) {
                return;
            }

            BoxCollider2D box = root.GetComponent<BoxCollider2D>();
            if (box == null) {
                box = root.AddComponent<BoxCollider2D>();
            }

            Vector2 ext = new Vector2(extXRaw / RawDiv, extYRaw / RawDiv) * Unity6ToUnity2017WorldScale;
            Vector2 off = new Vector2(offXRaw / RawDiv, offYRaw / RawDiv) * Unity6ToUnity2017WorldScale;
            box.size = ext * 2f;
            box.offset = off;
        }
    }
}
