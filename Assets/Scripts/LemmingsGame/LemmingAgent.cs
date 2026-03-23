using UnityEngine;

namespace Hakaton.Lemmings
{
    public sealed class LemmingAgent : MonoBehaviour
    {
        private const float Width = 0.56f;
        private const float Height = 0.9f;
        private const float WalkSpeed = 1.1f;
        private const float Gravity = 9.2f;
        private const float FallStep = 0.06f;
        private const float DigSpeed = 0.85f;
        private const float DigRadius = 0.46f;
        private const float AnimationRate = 0.22f;
        private static readonly Vector2 DigDirection = new Vector2(0.8660254f, -0.5f);

        private readonly Sprite[] walkSprites = new Sprite[2];
        private readonly Sprite[] digSprites = new Sprite[2];

        private SpriteRenderer spriteRenderer;
        private SpriteRenderer highlightRenderer;
        private float verticalVelocity;
        private float animationTimer;
        private int animationFrame;
        private TerrainMap terrainMap;
        private int currentDigPlatformId = -1;
        private float currentDigPlatformBottomY;
        private bool forceInitialFall;
        private bool isFalling;

        public int Direction { get; private set; } = 1;
        public bool IsAlive { get; private set; }
        public bool IsDigging { get; private set; }

        public Rect BoundsRect
        {
            get
            {
                Vector3 position = transform.position;
                return new Rect(position.x - Width * 0.5f, position.y, Width, Height);
            }
        }

        public void Initialize(TerrainMap terrain, Vector2 spawnPosition)
        {
            terrainMap = terrain;
            transform.position = spawnPosition;
            walkSprites[0] = PixelArtFactory.CreateLemmingWalkSprite(0);
            walkSprites[1] = PixelArtFactory.CreateLemmingWalkSprite(1);
            digSprites[0] = PixelArtFactory.CreateLemmingDigSprite(0);
            digSprites[1] = PixelArtFactory.CreateLemmingDigSprite(1);

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 10;
            spriteRenderer.sprite = walkSprites[0];

            GameObject highlightObject = new GameObject("Highlight");
            highlightObject.transform.SetParent(transform, false);
            highlightObject.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            highlightRenderer = highlightObject.AddComponent<SpriteRenderer>();
            highlightRenderer.sortingOrder = 9;
            highlightRenderer.sprite = PixelArtFactory.CreateHighlightSprite();
            highlightRenderer.enabled = false;

            IsAlive = true;
            animationTimer = 0f;
            animationFrame = 0;
            forceInitialFall = true;
            isFalling = true;
            verticalVelocity = 0f;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlightRenderer != null)
            {
                highlightRenderer.enabled = highlighted && IsAlive;
            }
        }

        public bool ContainsWorldPoint(Vector2 worldPoint)
        {
            return BoundsRect.Contains(worldPoint);
        }

        public void AssignPickaxe()
        {
            if (!IsAlive || IsDigging)
            {
                return;
            }

            Vector2 platformProbe = (Vector2)transform.position + new Vector2(0f, -0.05f);
            if (!terrainMap.TryGetPlatformBodyAtWorld(platformProbe, out currentDigPlatformId, out currentDigPlatformBottomY))
            {
                currentDigPlatformId = -1;
                currentDigPlatformBottomY = Mathf.Floor(transform.position.y - 0.02f);
            }

            IsDigging = true;
            forceInitialFall = false;
            isFalling = false;
            verticalVelocity = 0f;
            animationFrame = 0;
            animationTimer = 0f;
            RefreshSprite();
        }

        public void Step(float deltaTime)
        {
            if (!IsAlive)
            {
                return;
            }

            AdvanceAnimation(deltaTime);

            if (forceInitialFall || isFalling)
            {
                UpdateFalling(deltaTime);
            }
            else if (IsDigging)
            {
                UpdateDigging(deltaTime);
            }
            else
            {
                UpdateWalking(deltaTime);
            }

            RefreshSprite();
        }

        public void Kill()
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            Destroy(gameObject);
        }

        public void Save()
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            Destroy(gameObject);
        }

        private void AdvanceAnimation(float deltaTime)
        {
            animationTimer += deltaTime;
            if (animationTimer < AnimationRate)
            {
                return;
            }

            animationTimer -= AnimationRate;
            animationFrame = 1 - animationFrame;
        }

        private void UpdateWalking(float deltaTime)
        {
            verticalVelocity = 0f;
            float horizontalMove = Direction * WalkSpeed * deltaTime;
            Rect wallProbe = BuildWallProbe(horizontalMove);
            if (terrainMap.OverlapsSolid(wallProbe))
            {
                Direction *= -1;
                return;
            }

            Vector2 nextPosition = (Vector2)transform.position + new Vector2(horizontalMove, 0f);
            transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);

            if (!terrainMap.IsGrounded(nextPosition, Width))
            {
                isFalling = true;
            }
        }

        private void UpdateFalling(float deltaTime)
        {
            ResolveEmbeddedSolidForAirborneState();

            verticalVelocity -= Gravity * deltaTime;
            float remaining = Mathf.Abs(verticalVelocity * deltaTime);
            int steps = Mathf.Max(1, Mathf.CeilToInt(remaining / FallStep));
            float step = verticalVelocity * deltaTime / steps;

            for (int i = 0; i < steps; i++)
            {
                Rect nextRect = OffsetRect(BoundsRect, new Vector2(0f, step));
                if (!terrainMap.OverlapsSolid(nextRect))
                {
                    transform.position += new Vector3(0f, step, 0f);
                    continue;
                }

                verticalVelocity = 0f;
                IsDigging = false;
                forceInitialFall = false;
                isFalling = false;
                currentDigPlatformId = -1;
                return;
            }
        }

        private void UpdateDigging(float deltaTime)
        {
            Vector2 currentPosition = transform.position;
            Vector2 step = new Vector2(
                Direction * DigDirection.x * DigSpeed * deltaTime,
                DigDirection.y * DigSpeed * deltaTime);
            Rect sideProbe = OffsetRect(BoundsRect, new Vector2(Mathf.Sign(Direction) * 0.15f, 0f));

            if (terrainMap.OverlapsIndestructible(sideProbe))
            {
                Direction *= -1;
                step.x *= -1f;
            }

            Vector2 digTarget = currentPosition + new Vector2(step.x * 0.8f, step.y * 0.8f + 0.1f);
            terrainMap.DigCircle(digTarget, DigRadius);
            terrainMap.DigCircle(digTarget + new Vector2(0f, 0.3f), DigRadius);
            terrainMap.DigCircle(digTarget + new Vector2(Direction * 0.18f, 0.14f), DigRadius * 0.9f);

            Rect nextRect = OffsetRect(BoundsRect, step);
            if (!terrainMap.OverlapsIndestructible(nextRect))
            {
                transform.position = currentPosition + step;
            }

            bool pivotBelowBottomBoundary = transform.position.y < currentDigPlatformBottomY - 0.02f;
            bool noPlatformOverlap = !terrainMap.HasAnyDestructibleMaterialInRect(BoundsRect);
            if (pivotBelowBottomBoundary && noPlatformOverlap)
            {
                EnterBreakthroughFall();
            }
        }

        private void EnterBreakthroughFall()
        {
            IsDigging = false;
            forceInitialFall = false;
            isFalling = true;
            verticalVelocity = Mathf.Min(verticalVelocity, -0.35f);
            currentDigPlatformId = -1;
            ResolveEmbeddedSolidForAirborneState();
        }

        private void ResolveEmbeddedSolidForAirborneState()
        {
            if (!terrainMap.OverlapsSolid(BoundsRect))
            {
                return;
            }

            const int maxSteps = 80;
            const float stepSize = 0.02f;
            for (int i = 0; i < maxSteps && terrainMap.OverlapsSolid(BoundsRect); i++)
            {
                transform.position += new Vector3(0f, -stepSize, 0f);
            }
        }

        private void RefreshSprite()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = IsDigging ? digSprites[animationFrame] : walkSprites[animationFrame];
            spriteRenderer.flipX = Direction < 0;
        }

        private static Rect OffsetRect(Rect rect, Vector2 offset)
        {
            return new Rect(rect.position + offset, rect.size);
        }

        private Rect BuildWallProbe(float horizontalMove)
        {
            Rect bounds = BoundsRect;
            float probeWidth = Mathf.Max(0.04f, Mathf.Abs(horizontalMove) + 0.04f);
            float probeHeight = Height * 0.55f;
            float probeY = bounds.y + Height * 0.22f;
            float probeX = Direction > 0 ? bounds.xMax : bounds.xMin - probeWidth;
            return new Rect(probeX, probeY, probeWidth, probeHeight);
        }
    }
}
