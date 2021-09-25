using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;

[System.Serializable]
public struct WeightedRigidbodySetting
{
	public BodyPart bodyPart;
	public float multiplier;
}

public struct TimedMaxOffset
{
	public float time;
	public float maxOffset;
	public Vector3 maxOffsetBPCoord;
	public string maxOffBPName;
}

public struct TimedContacts
{
	public float time;
	public List<VelocityContact> contacts;
}

public struct TimedPositionedContactSet
{
	public float time;
	public Vector2 oldCenter;
	public List<Vector2> contactSet;
}

public struct VelocityContact
{
	/// <summary>
	/// Информация о контакте. Осторожно, содержит классы!
	/// Убрано, чтобы не вызывать потенциальных багов с пропавшими коллайдерами
	/// </summary>
	//public ContactPoint contact;
	/// <summary>
	/// Точка контакта
	/// </summary>
	public Vector3 contactPoint;
	/// <summary>
	/// Значение относительной скорости соударяющихся объектов в момент удара.
	/// </summary>
	public float velocity;
	/// <summary>
	/// Значение нашей скорости в момент удара.
	/// </summary>
	public float ourVelocity;
	/// <summary>
	/// Значение не нашей скорости в момент удара.
	/// </summary>
	public float otherVelocity;
	/// <summary>
	/// Наше местоположение в момент удара.
	/// </summary>
	public Vector3 ourCoord;
	/// <summary>
	/// Не наше местоположение в момент удара.
	/// </summary>
	public Vector3 otherCoord;
	/// <summary>
	/// Слой не нашего коллайдера. Сохраняем, потому что другой коллайдер может исчезнуть (взрыв например)
	/// </summary>
	public int otherLayer;
	/// <summary>
	/// Id нашего gameobject - Для сравнения с результатами других вычислений, например точек опоры
	/// </summary>
	public int ourId;
	/// <summary>
	/// Нормаль удара
	/// </summary>
	public Vector3 normal;
}

public enum LeftRight
{
	Left,
	Right
}

/// <summary>
/// Компонент, занимающийся наблюдением за равновесием персонажа.
/// </summary>
public class BalanceWatcher : MonoBehaviour
{
	#region debug vars
	[RootMotion.LargeHeader("Debug (Read Only)")]
	// для удобного наблюдения в эдиторе
	public float debugMass;
	public Vector3 debugCenter;
	public List<Vector2> debugProcessedContacts;
	public Vector2 debugMassCenterProcessed;
	public bool debugMassCenterInsideConvexHull;
	public float debugDistanceBetweenMassCenterAndConvexHull;
	public MoveDirection debugFallDirection;
	public float debugMinIntersection;
	public float debugMaxIntersection;
	public Rigidbody headDebug;
	#endregion

	#region properties
	// Все части тела
	private List<Rigidbody> BodyParts { get => partsDictionary.Rigidbodies; }
	// Все коллайдеры
	private List<Collider> Colliders { get => partsDictionary.Colliders; }


	// для остальных классов
	public Vector2 MassCenterProcessed { get => massCenterProcessed; }
	public Vector2 AnimationMassCenterProcessed { get => animationMassCenterProcessed; }
	public Vector2 GeneralImpulseProcessed { get => generalImpulseProcessed; }
	public Vector2 AnimationGeneralImpulseProcessed { get => animationGeneralImpulseProcessed; }
	public Vector3 PredictedMassCenterProcessed { get => futurePositionProcessed; }
	//public Vector3 AnimationPredictedMassCenterProcessed { get => futureAnimPositionProcessed; } // проверить на корректность
	public Vector2 ForwardProcessed { get => ProcessPoint(partsDictionary.characterTransform.forward); }
	public float MaxOnLine { get => maxIntersectionProceed; }
	public float MinOnLine { get => minIntersectionProceed; }
	public float InstantMaxOnLine { get => instantMaxIntersectionProceed; }
	public float InstantMinOnLine { get => instantMinIntersectionProceed; }
	public float MassCenterOnLine { get => massCenterIntersectionProceed; }
	public float FutureMassCenterOnLine { get => futureMassCenterIntersectionProceed; }
	public int IntersectionsCount { get => historicalContactsCount; }
	public float AbsoluteWeightedOffset { get => absoluteWeightedOffset; }
	public TimedMaxOffset MaxSavedOffset
	{
		get
		{
			TimedMaxOffset theMaxOffset = new TimedMaxOffset();
			foreach (var smo in savedMaxOffsets)
			{
				if (smo.maxOffset > theMaxOffset.maxOffset) theMaxOffset = smo;
			}
			return theMaxOffset;
		}
	}
	public bool WasHit { get => historicalObstacleContactsCount > 0 || hadPmHitThisFUpdate; }
	public float LastHitTime { get => lastHitTime; }
	public bool FreezeConvexHull { set; get; }
	public Vector3 MassCenter { get => massCenter; }
	public Vector3 AnimMassCenter { get => animationMassCenter; }
	public HashSet<RootMotion.Dynamics.Muscle> Fulcrums { get => fulcrumMuscles; }
	public HashSet<RootMotion.Dynamics.Muscle> FulcrumMusclesFilteredByNumOfContacts { get => fulcrumMusclesFilteredByNumOfContacts; }
	public float MaxPointsOffsetFromAnimation { get => maxOffsetFromAnimDistance; }
	public string PartWithTheMaxOffsetFromAnimation { get => maxOffsetFromAnimName; }
	public float MaxOffsetFromRod { get => maxOffsetFromRod; }
	public float MaxDissatisfactionOfAnimationFulcrums { get => maxFulcrumDissatisfactionOffset; }
	public string MaxFulcrumDissatisfactionBodyPartName { get => maxFulcrumDissatisfactionBodyPartName; }
	public bool AnimationHaveGround { get => animationHaveGround; }
	public LeftRight LeadLeg { get; private set; }
	public float DistanceToGround { get; private set; }
	public float OffsettedDistanceToGround { get; private set; }
	#endregion

	#region settings
	[RootMotion.LargeHeader("Settings")]
	public BodyPartsDictionary partsDictionary;
	public RootMotion.Dynamics.BehaviourPuppet puppetBehaviour;

	/// <summary>
	/// Важнейшая переменная класса.
	/// Внешняя сила, действующая на объект.
	/// В спокойной инерциальной системе этой силой выступает гравитация.
	/// В неинерциальной к гравитации добавляется внешнее ускорение системы.
	/// Все точки опоры ниже центра масс (и центр масс) проецируются на эту плоскость, 
	/// что превращает задачу в детекциию двумерного нахожения точки внутри фигуры.
	/// (что делать с точками выше центра масс, пока не понятно, но скорее всего они будут особыми случаями)
	/// </summary>
	public Vector3 ExternalForce;

	public float MaxOffsetMemoryTime;

	public LayerMask doNotRegisterContacts;

	public float predictionTime;

	public LayerMask hitInitiators;

	public LayerMask hitInitiatorsOnFulcrums;

	public bool usePointEnlargement;
	public float pointEnlargementRadius = 0.1f;
	public int pointEnlargementQuality = 6; // количество точек в круге

	public List<WeightedRigidbodySetting> pointsToCalcWeightedOffsetFromAnim;

	public List<BodyPart> bodyPartsToTraceOffsetFromCentralRod; // предполагаем, что координаты стержня совпадают с координатами objectTransform

	public LayerMask layersToStandOn;
	public float stabilizationOffset = 0;

	public string actionsAnimProp;
	public string actionsAnimStateName;
	public int[] curvedActionsIds;
	public string[] curvedAnimStatesNames;

	public float distanceToGroundOffsetForward;

	public float contactSetLifetime;
	public int contactSetSkips = 0;

	public bool logCollisions = false;
	public float logCollisionImpulseFilter;

	public bool useLegacyFulcrumsCurves = false;

	[HideInInspector]
	public int minContactsToCountFulcrumIn = 1;
	[HideInInspector]
	public int maxContactsToCountFulcrumIn = int.MaxValue;
	#endregion

	#region visualization settings
	[RootMotion.LargeHeader("Visualization")]
	// визуализация
	public bool doVisualize;
	public Transform visTransform;
	public SpriteRenderer visPointPrefab;
	public LineRenderer visLinePrefab;
	public LineRenderer hullLinePrefab;
	public SpriteRenderer visArrowPrefab;
	public Vector3 visOffset;
	public float visScale = 1;
	public float visPointScale = 1;
	public float visMinDistanceBwHullPoints = 0.1f;
	public Gradient visLineNormal;
	public Gradient visLineFrozen;
	public UnityEngine.UI.Text offsetsText;
	#endregion

	#region fields
	private readonly List<SpriteRenderer> visPoints = new List<SpriteRenderer>(); // TODO: Pool
	private LineRenderer visLine;
	private LineRenderer hullLine;
	private readonly Dictionary<MoveDirection, SpriteRenderer> visArrows = new Dictionary<MoveDirection, SpriteRenderer>();
	private readonly Dictionary<MoveDirection, Vector3> visArrowsRotation = new Dictionary<MoveDirection, Vector3>();

	private Vector3 basisUp;
	private Vector3 basisForward;
	private Vector3 basisRight;

	private float mass;
	private Vector3 massCenter;
	private Vector3 generalImpulse;
	private float animationMass;
	private Vector3 animationMassCenter;
	private Vector3 animationGeneralImpulse;
	private float absoluteWeightedOffset;
	private readonly List<Vector2> contactsProcessed = new List<Vector2>();
	private Vector2 massCenterProcessed;
	private Vector2 generalImpulseProcessed;
	private Vector2 animationMassCenterProcessed;
	private Vector2 animationGeneralImpulseProcessed;

	private List<Vector2> convexHullPoints = new List<Vector2>();
	private List<Vector2> instantConvexHullPoints = new List<Vector2>();
	private bool massCenterInsideConvexHull;
	private float distanceBwMassCenterAndConvex;
	private Vector2 closestPointToMassCenterOnConvex;
	private Vector2 directionLineStart;
	private Vector2 directionLineEnd;
	private readonly List<Vector2> dirLineConvHullIntersections = new List<Vector2>();
	private readonly List<float> dirLineConvHullIntersectionsProceed = new List<float>();
	private readonly List<Vector2> dirLineInstantConvHullIntersections = new List<Vector2>();
	private readonly List<float> dirLineInstantConvHullIntersectionsProceed = new List<float>();
	private float minIntersectionProceed;
	private float maxIntersectionProceed;
	private float instantMinIntersectionProceed;
	private float instantMaxIntersectionProceed;
	private float futureMassCenterIntersectionProceed;
	private float massCenterIntersectionProceed;
	private MoveDirection fallDirection;
	private int historicalContactsCount;
	private int historicalObstacleContactsCount;
	private int additionalHistoricalObstacleContactsCount = 0;
	private float lastHitTime = -5;
	private bool hadPmHitThisFUpdate = false;

	private Vector2 lastUnfrozenAnimationMassCenter;
	private readonly List<Vector2> lastUnfrozenConvexHull = new List<Vector2>();

	private readonly Queue<TimedMaxOffset> savedMaxOffsets = new Queue<TimedMaxOffset>();
	private readonly List<VelocityContact> contacts = new List<VelocityContact>();
	private readonly List<VelocityContact> filteredContacts = new List<VelocityContact>();

	private Vector3 futurePosition;
	private Vector2 futurePositionProcessed;
	//private Vector3 futureAnimPosition; // проверить на корректность
	//private Vector2 futureAnimPositionProcessed; // проверить на корректность

	private readonly HashSet<int> fulcrumIds = new HashSet<int>();
	private readonly HashSet<RootMotion.Dynamics.Muscle> fulcrumMuscles = new HashSet<RootMotion.Dynamics.Muscle>();
	private readonly HashSet<RootMotion.Dynamics.Muscle> fulcrumMusclesFilteredByNumOfContacts = new HashSet<RootMotion.Dynamics.Muscle>();

	private float maxOffsetFromAnimDistance;
	private string maxOffsetFromAnimName;
	private readonly Dictionary<string, Tuple<float, float>> maxOffsets = new Dictionary<string, Tuple<float, float>>();
	private float maxOffsetFromRod;

	private float maxFulcrumDissatisfactionOffset;
	private string maxFulcrumDissatisfactionBodyPartName;
	private bool animationHaveGround;

	// время, координата центра при создании, собственно оно
	private readonly List<TimedPositionedContactSet> contactSets = new List<TimedPositionedContactSet>();

	private readonly Dictionary<string, Transform> findBoneWithName_cache = new Dictionary<string, Transform>();
	#endregion

	#region unity
	private void Awake()
	{
		InitCollisionListener();
	}

	private void Start()
	{
		if (doVisualize) InitLines();
	}

	private void FixedUpdate()
	{
		ProcessFilteredContacts();
		FindFulcrums();
		CalculateMassCenter();
		CalculateAnimationMassCenter();
		CalculateGeneralImpulse();
		CalculateAnimationGeneralImpulse();
		CalculateMaxOffsetFromSelectedPoints();
		CalculateMaxoffsetFromRod();
		PredictPosition();
		//PredictAnimPosition(); // проверить на корректность
		CalculateAbsoluteWeightedOffset();
		SaveMaxOffset();
		CleanUpOldMaxOffsets();
		ProcessContactsAndOtherPoints();
		UpdateConvexHull();
		FindIfAnimationFulcrumsAreSatisfied();
		CalculateDirectionLine();
		FindIntersections();
		ProcessIntersections();
		CheckMassCenterInsideConvexHull();
		FindDistanceToConvexHull();
		DetermineFallDirection();
		UpdateLeadLeg();
		UpdateDistanceToGround();
		FillDebugData();
	}

	private void Update()
	{
		if (doVisualize) DrawVisualisation();
	}
	#endregion

	#region initialization
	private void InitColliderBridges()
	{
		foreach (var c in Colliders)
		{
			var bridge = c.gameObject.AddComponent<BalanceWatcherColliderBridge>();
			bridge.collisionEvent.AddListener(RegisterContacts);
		}
	}

	private void InitCollisionListener()
	{
		puppetBehaviour.OnCollision += OnPMCollision;
		puppetBehaviour.OnPreMuscleHit += OnPMHit;
		puppetBehaviour.OnCollisionImpulse += OnPMCollisionImpulse;
	}

	private void InitArrows()
	{
		visArrows[MoveDirection.Up] = Instantiate(visArrowPrefab, visTransform);
		visArrows[MoveDirection.Right] = Instantiate(visArrowPrefab, visTransform);
		visArrows[MoveDirection.Down] = Instantiate(visArrowPrefab, visTransform);
		visArrows[MoveDirection.Left] = Instantiate(visArrowPrefab, visTransform);

		Vector3 baseRot = visArrows[MoveDirection.Right].transform.rotation.eulerAngles;

		visArrowsRotation[MoveDirection.Right] = new Vector3(0, 0, 0) + baseRot;
		visArrowsRotation[MoveDirection.Up] = new Vector3(0, 0, 90) + baseRot;
		visArrowsRotation[MoveDirection.Left] = new Vector3(0, 0, 180) + baseRot;
		visArrowsRotation[MoveDirection.Down] = new Vector3(0, 0, 270) + baseRot;
	}

	private void InitLines()
	{
		visLine = Instantiate(visLinePrefab, visTransform);
		visLine.transform.position = visOffset;
		hullLine = Instantiate(hullLinePrefab, visTransform);
		hullLine.transform.position = visOffset;
	}

	#endregion

	#region visualization
	private void DrawVisualisation()
	{
		DrawPoints();
		DrawHull();
		DrawDirection();
		//DrawClosest();
		//DrawDirections();
	}

	private void DrawPoints()
	{
		foreach (var vp in visPoints)
		{
			Destroy(vp.gameObject);
		}
		visPoints.Clear();

		foreach (var cp in contactsProcessed)
		{
			AddVisPoint(cp, Color.green);
		}

		foreach (var i in dirLineConvHullIntersections)
		{
			AddVisPoint(i, Color.magenta);
		}

		AddVisPoint(futurePositionProcessed, Color.blue);
		AddVisPoint(animationMassCenterProcessed, Color.red);
	}

	private void AddVisPoint(Vector2 bCoord, Color color)
	{
		var np = Instantiate(visPointPrefab, visTransform);
		visPoints.Add(np);
		np.color = color;
		np.transform.localPosition = visScale * (Vector3.forward * bCoord.x + Vector3.right * bCoord.y) + visOffset + Vector3.up * partsDictionary.characterTransform.position.y;
		np.gameObject.transform.localScale = new Vector3(visPointScale, visPointScale, visPointScale);
	}

	private void DrawHull()
	{
		// рисование с помощью LineRenderer - тольк окружающая линия
		hullLine.positionCount = convexHullPoints.Count;
		List<Vector3> hps = new List<Vector3>();
		foreach (var chp in convexHullPoints) hps.Add(new Vector3(chp.y, partsDictionary.characterTransform.position.y, chp.x));
		hullLine.SetPositions(hps.ToArray());
		hullLine.colorGradient = FreezeConvexHull ? visLineFrozen : visLineNormal;

		// рисование с помощью SpriteShape - теряет точки
		/*visShape.transform.position = new Vector3(massCenterProcessed.y, visShapeYPosition, massCenterProcessed.x) + visOffset;

		visShape.spline.Clear();
		int j = 0;
		for (int i = 0; i < convexHullPoints.Count; i++)
			if (i == 0 || (convexHullPoints[i] - convexHullPoints[i - 1]).magnitude > visMinDistanceBwHullPoints)
				visShape.spline.InsertPointAt(j++, convexHullPoints[i] - new Vector2(massCenterProcessed.x, massCenterProcessed.y));*/
	}

	private void DrawDirection()
	{
		if (dirLineConvHullIntersections.Count() == 2)
		{
			visLine.gameObject.SetActive(true);
			visLine.SetPositions(new Vector3[] {
				new Vector3(dirLineConvHullIntersections[0].y, partsDictionary.characterTransform.position.y, dirLineConvHullIntersections[0].x),
				new Vector3(dirLineConvHullIntersections[1].y, partsDictionary.characterTransform.position.y, dirLineConvHullIntersections[1].x) });
		}
	}

	private void DrawClosest()
	{
		if (massCenterInsideConvexHull || convexHullPoints.Count < 2)
		{
			visLine.gameObject.SetActive(false);
		}
		else
		{
			visLine.gameObject.SetActive(true);
			visLine.SetPositions(new Vector3[] {
				new Vector3(massCenterProcessed.y, 0, massCenterProcessed.x),
				new Vector3(closestPointToMassCenterOnConvex.y, 0, closestPointToMassCenterOnConvex.x) });
		}
	}

	private void DrawDirections()
	{
		foreach (var va in visArrows)
		{
			va.Value.transform.position = visOffset + partsDictionary.characterTransform.position + new Vector3(1, 0, 0);
			va.Value.transform.eulerAngles = visArrowsRotation[va.Key] + Vector3.up * partsDictionary.characterTransform.rotation.eulerAngles.y;
			va.Value.color = Color.white;
		}
		if (fallDirection != MoveDirection.None) visArrows[fallDirection].color = Color.red;
	}
	#endregion

	#region helpers
	// https://stackoverflow.com/questions/2049582/how-to-determine-if-a-point-is-in-a-2d-triangle
	static float sign(Vector2 p1, Vector2 p2, Vector2 p3)
	{
		return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
	}

	// https://stackoverflow.com/questions/2049582/how-to-determine-if-a-point-is-in-a-2d-triangle
	static bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
	{
		float d1, d2, d3;
		bool has_neg, has_pos;

		d1 = sign(pt, v1, v2);
		d2 = sign(pt, v2, v3);
		d3 = sign(pt, v3, v1);

		has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
		has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

		return !(has_neg && has_pos);
	}

	// http://e-maxx.ru/algo/convex_hull_graham
	static bool cw(Vector2 a, Vector2 b, Vector2 c)
	{
		return a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y) < 0;
	}

	// http://e-maxx.ru/algo/convex_hull_graham
	static bool ccw(Vector2 a, Vector2 b, Vector2 c)
	{
		return a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y) > 0;
	}

	// Метод Грэхэма, самый эффективный
	// http://e-maxx.ru/algo/convex_hull_graham
	static List<Vector2> ConvexHull(List<Vector2> a)
	{
		List<Vector2> r = new List<Vector2>();
		for (int i = 0; i < a.Count; i++) r.Add(a[i]);
		if (r.Count == 1) return r;
		r.Sort(new Vec2Cmprr());
		Vector2 p1 = r[0], p2 = r[r.Count - 1];
		List<Vector2> up = new List<Vector2>(), down = new List<Vector2>();
		up.Add(p1);
		down.Add(p1);
		for (int i = 1; i < r.Count; ++i)
		{
			if (i == r.Count - 1 || cw(p1, r[i], p2))
			{
				while (up.Count >= 2 && !cw(up[up.Count - 2], up[up.Count - 1], r[i]))
					up.RemoveAt(up.Count - 1);
				up.Add(r[i]);
			}
			if (i == r.Count - 1 || ccw(p1, r[i], p2))
			{
				while (down.Count >= 2 && !ccw(down[down.Count - 2], down[down.Count - 1], r[i]))
					down.RemoveAt(down.Count - 1);
				down.Add(r[i]);
			}
		}
		r.Clear();
		for (int i = 0; i < up.Count; ++i)
			r.Add(up[i]);
		for (int i = down.Count - 2; i > 0; --i)
			r.Add(down[i]);
		return r;
	}

	// http://thirdpartyninjas.com/blog/2008/10/07/line-segment-intersection/
	static private bool ProcessIntersection(Vector2 point1, Vector2 point2, Vector2 point3, Vector2 point4, out Vector2 intersectionPoint)
	{
		intersectionPoint = new Vector2();

		float ua = (point4.x - point3.x) * (point1.y - point3.y) - (point4.y - point3.y) * (point1.x - point3.x);
		float ub = (point2.x - point1.x) * (point1.y - point3.y) - (point2.y - point1.y) * (point1.x - point3.x);
		float denominator = (point4.y - point3.y) * (point2.x - point1.x) - (point4.x - point3.x) * (point2.y - point1.y);

		bool intersection = false;

		if (Math.Abs(denominator) <= 0.00001f)
		{
			if (Math.Abs(ua) <= 0.00001f && Math.Abs(ub) <= 0.00001f)
			{
				intersection = true;
				intersectionPoint = (point1 + point2) / 2;
			}
		}
		else
		{
			ua /= denominator;
			ub /= denominator;

			if (ua >= 0 && ua <= 1 && ub >= 0 && ub <= 1)
			{
				intersection = true;
				intersectionPoint.x = point1.x + ua * (point2.x - point1.x);
				intersectionPoint.y = point1.y + ua * (point2.y - point1.y);
			}
		}

		return intersection;
	}

	// https://stackoverflow.com/a/58887364
	static Transform RecursiveFindChild(Transform parent, string childName)
	{
		foreach (Transform child in parent)
		{
			if (child.name == childName)
			{
				return child;
			}
			else
			{
				Transform found = RecursiveFindChild(child, childName);
				if (found != null)
				{
					return found;
				}
			}
		}
		return null;
	}

	static private bool PointInsideConvexHull(List<Vector2> hull, Vector2 point)
	{
		if (hull.Count < 3)
			return false;
		else
		{
			for (int i = 2; i < hull.Count; i++)
			{
				if (PointInTriangle(point, hull[0], hull[i - 1], hull[i]))
				{
					return true;
				}
			}
			return false;
		}
	}
	#endregion

	#region contacts
	public void IncreaseContactCount()
	{
		additionalHistoricalObstacleContactsCount++;
	}

	private void OnPMCollision(RootMotion.Dynamics.MuscleCollision collision)
	{
		ContactPoint[] contacts = new ContactPoint[collision.collision.contactCount];
		collision.collision.GetContacts(contacts);
		RegisterContacts(contacts, collision.collision.relativeVelocity.magnitude);
	}

	private void OnPMCollisionImpulse(RootMotion.Dynamics.MuscleCollision collision, float impulse)
	{
		if (logCollisions && impulse > logCollisionImpulseFilter)
		{
			Debug.Log($"Collision: {partsDictionary.puppetMaster.muscles[collision.muscleIndex].name}, {impulse}");
		}
	}

	private void OnPMHit(RootMotion.Dynamics.MuscleHit hit)
	{
		hadPmHitThisFUpdate = true;
	}

	private void RegisterContacts(ContactPoint[] cps, float vel)
	{
		foreach (var cp in cps)
		{
			contacts.Add(new VelocityContact
			{
				contactPoint = cp.point,
				velocity = vel,
				ourVelocity = cp.thisCollider.attachedRigidbody.velocity.magnitude,
				otherVelocity = cp.otherCollider.attachedRigidbody != null ? cp.otherCollider.attachedRigidbody.velocity.magnitude : 0,
				ourCoord = cp.thisCollider.transform.position,
				otherCoord = cp.otherCollider.transform.position,
				otherLayer = cp.otherCollider.gameObject.layer,
				ourId = cp.thisCollider.gameObject.GetInstanceID(),
				normal = cp.normal
			});
		}
	}

	private void ProcessFilteredContacts()
	{
		filteredContacts.Clear();
		foreach (var c in contacts)
		{
			if ((doNotRegisterContacts.value & (1 << c.otherLayer)) == 0)
				filteredContacts.Add(c);
		}
	}

	private void ProcessContactsAndOtherPoints()
	{
		historicalObstacleContactsCount = additionalHistoricalObstacleContactsCount;
		foreach (var c in contacts)
		{
			if ((hitInitiatorsOnFulcrums.value & (1 << c.otherLayer)) != 0 ||
				(hitInitiators.value & (1 << c.otherLayer)) != 0 &&
				!fulcrumIds.Contains(c.ourId))
				historicalObstacleContactsCount++;
		}
		additionalHistoricalObstacleContactsCount = 0;

		if (WasHit) lastHitTime = Time.time;

		contactsProcessed.Clear();

		foreach (var c in filteredContacts)
		{

			var cp = c.contactPoint;
			contactsProcessed.Add(ProcessPoint(cp));
		}
		historicalContactsCount = filteredContacts.Count();
		massCenterProcessed = ProcessPoint(massCenter);
		animationMassCenterProcessed = ProcessPoint(animationMassCenter);
		generalImpulseProcessed = ProcessPoint(generalImpulse);
		animationGeneralImpulseProcessed = ProcessPoint(animationGeneralImpulse);
		futurePositionProcessed = ProcessPoint(futurePosition);
		//futureAnimPositionProcessed = ProcessPoint(futureAnimPosition); // проверить на корректность

		var contactSet = new TimedPositionedContactSet();
		contactSet.time = Time.time;
		contactSet.oldCenter = animationMassCenterProcessed;
		contactSet.contactSet = new List<Vector2>();
		contactSet.contactSet.AddRange(contactsProcessed);
		contactSets.Add(contactSet);

		List<VelocityContact> histContacts = new List<VelocityContact>(filteredContacts.Count);
		histContacts.AddRange(filteredContacts);

		contacts.Clear();
		hadPmHitThisFUpdate = false;
	}

	private void UpdateConvexHull()
	{
		contactSets.RemoveAll(x => Time.time - x.time > contactSetLifetime);

		List<Vector2> instantPointsToHullRaw = new List<Vector2>();
		if (contactSets.Count() > 0)
		{
			var lastContactSetTime = contactSets.Max(x => x.time);
			if (Time.time - lastContactSetTime < float.Epsilon)
			{
				instantPointsToHullRaw.AddRange(contactSets.First(x => x.time == lastContactSetTime).contactSet);
			}
		}

		instantConvexHullPoints = CreateConvexHullFromRawPoints(instantPointsToHullRaw);


		if (FreezeConvexHull)
		{
			Vector2 animCMOffsetFromUnfrozen = animationMassCenterProcessed - lastUnfrozenAnimationMassCenter;
			convexHullPoints.Clear();
			foreach (var chp in lastUnfrozenConvexHull)
			{
				convexHullPoints.Add(chp + animCMOffsetFromUnfrozen);
			}
		}
		else
		{
			List<Vector2> pointsToHullRaw = new List<Vector2>();
			int i = 0;
			foreach (var cs in contactSets)
			{
				if (i % (contactSetSkips + 1) == 0)
				{
					Vector2 offset = animationMassCenterProcessed - cs.oldCenter;
					foreach (var p in cs.contactSet)
					{
						pointsToHullRaw.Add(p + offset);
					}
				}
				i++;
			}

			convexHullPoints = CreateConvexHullFromRawPoints(pointsToHullRaw);

			lastUnfrozenConvexHull.Clear();
			lastUnfrozenConvexHull.AddRange(convexHullPoints);
			lastUnfrozenAnimationMassCenter = animationMassCenterProcessed;
		}
	}

	private List<Vector2> CreateConvexHullFromRawPoints(List<Vector2> pointsToHullRaw)
	{
		List<Vector2> pointsToHull = new List<Vector2>();
		if (usePointEnlargement)
		{
			foreach (var p in pointsToHullRaw)
			{
				for (int i = 0; i < pointEnlargementQuality; i++)
				{
					var rv = new Vector2(Mathf.Cos(Mathf.Lerp(0, 2 * Mathf.PI, (float)i / pointEnlargementQuality)),
												 Mathf.Sin(Mathf.Lerp(0, 2 * Mathf.PI, (float)i / pointEnlargementQuality)));
					pointsToHull.Add(p + rv * pointEnlargementRadius);
				}
			}
		}
		else
		{
			pointsToHull.AddRange(pointsToHullRaw);
		}

		List<Vector2> res;
		if (pointsToHull.Count > 0)
		{
			res = ConvexHull(pointsToHull);
		}
		else
		{
			res = new List<Vector2>();
		}

		return res;
	}
	#endregion

	#region getters
	private float GetBPOffsetFromRod(Rigidbody bp)
	{
		return Vector2.Distance(new Vector2(bp.position.x, bp.position.z), new Vector2(partsDictionary.characterTransform.position.x, partsDictionary.characterTransform.position.z));
	}

	public Vector3 GetBPCoordWithMaxOffset()
	{
		return GetBPCoordWithMaxOffset(out float meh, out string mehmeh);
	}

	public Vector3 GetBPCoordWithMaxOffset(out float maxOff, out string maxOffBPName)
	{
		float maxOffset = 0;
		Rigidbody maxBp = null;
		foreach (var bp in BodyParts)
		{
			if (bp.gameObject.activeInHierarchy)
			{
				float offset = GetBPOffsetModule(bp);
				if (offset > maxOffset)
				{
					maxOffset = offset;
					maxBp = bp;
				}
			}
		}
		maxOff = maxOffset;
		maxOffBPName = maxBp == null ? "None" : maxBp.name;
		if (maxBp == null)
		{
			return new Vector3(0, 0, 0);
		}
		else
		{
			return maxBp.transform.position - massCenter;
		}
	}

	private float GetBPOffsetModule(Rigidbody bp)
	{
		return Vector3.Distance(GetBPCoord(bp), GetBPAnimCoord(bp));
	}

	private Vector3 GetBPCoord(Rigidbody bp)
	{
		return bp.transform.position;
	}

	private Vector3 GetBPAnimCoord(Rigidbody bp)
	{
		Transform bone = FindBoneWithName(bp.name);
		Vector3 targetPos = partsDictionary.puppetMaster.GetMuscle(bone).targetAnimatedPosition;
		return targetPos;
	}

	private Transform FindBoneWithName(string name)
	{
		if (!findBoneWithName_cache.ContainsKey(name)) findBoneWithName_cache[name] = RecursiveFindChild(partsDictionary.characterTransform, name);
		return findBoneWithName_cache[name];
	}

	private float GetBpAbsWeightedOffset(Rigidbody bp)
	{
		return GetBPOffsetModule(bp) * bp.mass;
	}
	#endregion

	#region projections
	/// <summary>
	/// Перерабатывает точку в удобный для работы двумерный формат.
	/// По факту, переводит любую точку в проекцию "на полу".
	/// </summary>
	public Vector2 ProcessPoint(Vector3 point)
	{
		// Это наш базис, вектор наверх представляет внешнюю силу
		basisUp = ExternalForce;
		basisForward = Vector3.forward;
		basisRight = Vector3.right;

		return PointProjection(point, basisUp, basisForward, basisRight);
	}

	/// <summary>
	/// Скорее всего работает некорректно...
	/// Переводит точку в её проекцию на сфере.
	/// Должно:
	/// считать за ноль самую верхушку сферы
	/// ориентироваться на направление вперёд при нахождении координаты y
	/// ориентироваться на направление вправо при нахождении координаты x
	/// считать за радиус расстояние между центром и точкой
	/// </summary>
	public Vector2 GetPointAngle(Vector3 point, Vector3 cphereCenter)
	{
		Vector3 forward = partsDictionary.characterTransform.forward;
		Vector3 up = Vector3.up;
		Vector3 right = partsDictionary.characterTransform.right;

		float rad = (point - cphereCenter).magnitude;
		Vector3 zero = up * rad + cphereCenter;
		Vector3 pointOffsetFromZero = point - zero;
		float forwardProjection = Vector3.Dot(forward, pointOffsetFromZero);
		float rightProjection = Vector3.Dot(right, pointOffsetFromZero);
		float upProjection = Vector3.Dot(up, pointOffsetFromZero);
		float forwardAngle = Mathf.Atan2(forwardProjection, rad);
		float rightAngle = Mathf.Atan2(rightProjection, rad);
		bool isUp = upProjection < 0;
		if (!isUp)
		{
			forwardAngle *= -1;
			rightAngle *= -1;
		}
		return new Vector2(forwardAngle, rightAngle);
	}

	/// <summary>
	/// Переводит точку в проекцию "на чувака спереди"
	/// </summary>
	/// <param name="point"></param>
	/// <returns></returns>
	public Vector2 ProcessPointVertically(Vector3 point)
	{
		return PointProjection(point, partsDictionary.characterTransform.forward, partsDictionary.characterTransform.right, partsDictionary.characterTransform.up);
	}

	/// <summary>
	/// Проекция точки на плоскость, перпендикулярную basisUp.
	/// </summary>
	/// <param name="point"></param>
	/// <param name="basisUp"></param>
	/// <param name="basisForward"></param>
	/// <param name="basisRight"></param>
	/// <returns></returns>
	private Vector2 PointProjection(Vector3 point, Vector3 basisUp, Vector3 basisForward, Vector3 basisRight)
	{
		Vector3.OrthoNormalize(ref basisUp, ref basisForward, ref basisRight);

		// точка представляется в виде проекции на плоскость, ортогональную вектору внешней силы
		// базис той плоскости - векторы forward и right
		Vector2 result = new Vector2(Vector3.Dot(point, basisForward),
										  Vector3.Dot(point, basisRight));
		return result;
	}
	#endregion

	#region calculations
	private void CalculateMaxoffsetFromRod()
	{
		maxOffsetFromRod = 0;
		foreach (var bp in bodyPartsToTraceOffsetFromCentralRod)
		{
			maxOffsetFromRod = Mathf.Max(maxOffsetFromRod, GetBPOffsetFromRod(GetRigidbodyFromBodyPart(bp)));
		}
	}

	private void CalculateMaxOffsetFromSelectedPoints()
	{
		maxOffsetFromAnimDistance = 0;
		maxOffsetFromAnimName = "";

		foreach (var p in pointsToCalcWeightedOffsetFromAnim)
		{
			Rigidbody r = GetRigidbodyFromBodyPart(p.bodyPart);
			var po = p.multiplier * GetBPOffsetModule(r);
			if (po > maxOffsetFromAnimDistance)
			{
				maxOffsetFromAnimDistance = po;
				maxOffsetFromAnimName = r.name;
			}
			if (!maxOffsets.ContainsKey(r.name))
			{
				maxOffsets[r.name] = new Tuple<float, float>(Mathf.Round(GetBPOffsetModule(r) * 100) / 100, Mathf.Round(po * 100) / 100);
			}
			else
			{
				if (maxOffsets[r.name].Item1 < Mathf.Round(GetBPOffsetModule(r) * 100) / 100)
				{
					maxOffsets[r.name] = new Tuple<float, float>(Mathf.Round(GetBPOffsetModule(r) * 100) / 100, Mathf.Round(po * 100) / 100);
				}
			}
		}
		if (offsetsText != null)
		{
			string offsets = "";
			foreach (var m in maxOffsets)
			{
				offsets += $"{m.Key}:\t\t{m.Value.Item1};\t\t{m.Value.Item2}\n";
			}
			offsetsText.text = offsets;
		}
	}

	private Rigidbody GetRigidbodyFromBodyPart(BodyPart bp)
	{
		return bp switch
		{
			BodyPart.Head => partsDictionary.Head,
			BodyPart.Hips => partsDictionary.Hips,
			BodyPart.LeftArm => partsDictionary.LeftArm,
			BodyPart.LeftFoot => partsDictionary.LeftFoot,
			BodyPart.LeftForeArm => partsDictionary.LeftForeArm,
			BodyPart.LeftHand => partsDictionary.LeftHand,
			BodyPart.LeftLeg => partsDictionary.LeftLeg,
			BodyPart.LeftUpLeg => partsDictionary.LeftUpLeg,
			BodyPart.RightArm => partsDictionary.RightArm,
			BodyPart.RightFoot => partsDictionary.RightFoot,
			BodyPart.RightForeArm => partsDictionary.RightForeArm,
			BodyPart.RightHand => partsDictionary.RightHand,
			BodyPart.RightLeg => partsDictionary.RightLeg,
			BodyPart.RightUpLeg => partsDictionary.RightUpLeg,
			BodyPart.Spine => partsDictionary.Spine,
			_ => null,
		};
	}

	public void ClearMaxOffset()
	{
		maxOffsets.Clear();
	}

	private void CalculateAbsoluteWeightedOffset()
	{
		absoluteWeightedOffset = 0;
		foreach (var bp in BodyParts)
		{
			if (bp.gameObject.activeInHierarchy)
			{
				float absOffset = GetBpAbsWeightedOffset(bp);
				absoluteWeightedOffset += absOffset;
			}
		}
	}

	private void CalculateMassCenter()
	{
		mass = 0;
		massCenter = new Vector3(0, 0, 0);
		foreach (var bp in BodyParts)
		{
			if (bp.gameObject.activeInHierarchy)
			{
				massCenter = (massCenter * mass + bp.transform.position * bp.mass) / (mass + bp.mass);
				mass += bp.mass;
			}
		}
	}

	private void CalculateAnimationMassCenter()
	{
		animationMass = 0;
		animationMassCenter = new Vector3(0, 0, 0);
		foreach (var bp in BodyParts)
		{
			if (bp.gameObject.activeInHierarchy)
			{
				Transform bone = FindBoneWithName(bp.name);
				if (bone == null)
				{
					Debug.LogWarning($"Balance Watcher не смог найти анимационную кость с именем {bp.name}. Центр масс будет вычислен с ошибками.");
				}
				else
				{
					Vector3 targetPos = partsDictionary.puppetMaster.GetMuscle(bone).targetAnimatedPosition;
					animationMassCenter = (animationMassCenter * animationMass + targetPos * bp.mass) / (animationMass + bp.mass);
					animationMass += bp.mass;
				}
			}
		}
	}

	private void CalculateGeneralImpulse()
	{
		generalImpulse = new Vector3(0, 0, 0);
		foreach (var bp in BodyParts)
		{
			if (bp.gameObject.activeInHierarchy)
			{
				generalImpulse += bp.velocity * bp.mass;
			}
		}
	}

	private void CalculateAnimationGeneralImpulse()
	{
		animationGeneralImpulse = new Vector3(0, 0, 0);
		foreach (var bp in BodyParts)
		{
			if (bp.gameObject.activeInHierarchy)
			{
				Transform bone = FindBoneWithName(bp.name);
				Vector3 targetVel = partsDictionary.puppetMaster.GetMuscle(bone).targetVelocity;
				animationGeneralImpulse += targetVel * bp.mass;
			}
		}
	}

	private void PredictPosition()
	{
		futurePosition = massCenter + generalImpulse * predictionTime / mass - partsDictionary.characterRigidbody.velocity * predictionTime;
	}

	/*private void PredictAnimPosition() // проверить на корректность
	{
		futureAnimPosition = animationMassCenter + animationGeneralImpulse * predictionTime / animationMass;
	}*/
	#endregion

	#region direction line
	private void CalculateDirectionLine()
	{
		Vector2 A = animationMassCenterProcessed;
		Vector2 B = futurePositionProcessed;
		Vector2 Delta = B - A;
		directionLineStart = A - Delta.normalized * 100;
		directionLineEnd = A + Delta.normalized * 100;
	}

	private void ProcessIntersections()
	{
		dirLineConvHullIntersectionsProceed.Clear();
		foreach (var i in dirLineConvHullIntersections)
		{
			dirLineConvHullIntersectionsProceed.Add(Vector2.Dot((directionLineEnd - directionLineStart).normalized, i - animationMassCenterProcessed));
		}
		if (dirLineConvHullIntersectionsProceed.Count > 0)
		{
			minIntersectionProceed = dirLineConvHullIntersectionsProceed.Min();
			maxIntersectionProceed = dirLineConvHullIntersectionsProceed.Max();
		}
		else
		{
			minIntersectionProceed = 0;
			maxIntersectionProceed = 0;
		}

		massCenterIntersectionProceed = Vector2.Dot((directionLineEnd - directionLineStart).normalized, massCenterProcessed - animationMassCenterProcessed); // не совсем точно, линия взята от предсказания
		futureMassCenterIntersectionProceed = Vector2.Dot((directionLineEnd - directionLineStart).normalized, futurePositionProcessed - animationMassCenterProcessed);


		dirLineInstantConvHullIntersectionsProceed.Clear();
		foreach (var i in dirLineInstantConvHullIntersections)
		{
			dirLineInstantConvHullIntersectionsProceed.Add(Vector2.Dot((directionLineEnd - directionLineStart).normalized, i - animationMassCenterProcessed));
		}
		if (dirLineInstantConvHullIntersectionsProceed.Count > 0)
		{
			instantMinIntersectionProceed = dirLineInstantConvHullIntersectionsProceed.Min();
			instantMaxIntersectionProceed = dirLineInstantConvHullIntersectionsProceed.Max();
		}
		else
		{
			instantMinIntersectionProceed = 0;
			instantMaxIntersectionProceed = 0;
		}
	}

	private void FindIntersections()
	{
		dirLineConvHullIntersections.Clear();
		for (int i = 0; i < convexHullPoints.Count - 1; i++)
		{
			FindIntersection(convexHullPoints[i], convexHullPoints[i + 1], dirLineConvHullIntersections);
		}
		if (convexHullPoints.Count > 2) FindIntersection(convexHullPoints[0], convexHullPoints[convexHullPoints.Count - 1], dirLineConvHullIntersections);

		dirLineInstantConvHullIntersections.Clear();
		for (int i = 0; i < instantConvexHullPoints.Count - 1; i++)
		{
			FindIntersection(instantConvexHullPoints[i], instantConvexHullPoints[i + 1], dirLineInstantConvHullIntersections);
		}
		if (instantConvexHullPoints.Count > 2) FindIntersection(instantConvexHullPoints[0], instantConvexHullPoints[instantConvexHullPoints.Count - 1], dirLineInstantConvHullIntersections);
	}

	private void FindIntersection(Vector2 s, Vector2 e, List<Vector2> output)
	{
		Vector2 intersection;
		if (ProcessIntersection(directionLineStart, directionLineEnd, s, e, out intersection))
		{
			output.Add(intersection);
		}
	}
	#endregion

	#region max offset
	private void SaveMaxOffset()
	{
		TimedMaxOffset timedMaxOffset = new TimedMaxOffset();
		timedMaxOffset.time = Time.time;
		timedMaxOffset.maxOffsetBPCoord = GetBPCoordWithMaxOffset(out timedMaxOffset.maxOffset, out timedMaxOffset.maxOffBPName);
		savedMaxOffsets.Enqueue(timedMaxOffset);
	}

	private void CleanUpOldMaxOffsets()
	{
		while (savedMaxOffsets.Count() > 0 && Time.time - savedMaxOffsets.Peek().time > MaxOffsetMemoryTime) savedMaxOffsets.Dequeue();
	}
	#endregion

	#region fulcrums
	private void FindFulcrums()
	{
		fulcrumIds.Clear();
		fulcrumMuscles.Clear();
		fulcrumMusclesFilteredByNumOfContacts.Clear();
		if (Colliders.Count > 4)
		{
			foreach (var c in Colliders)
			{
				int contactsInCollider = 0;
				foreach (var fc in filteredContacts)
				{
					if (fc.ourId == c.gameObject.GetInstanceID())
					{
						if (Vector3.Angle(fc.normal, Vector3.up) < 5)
						{
							contactsInCollider++;
						}
					}
				}

				if (contactsInCollider > 0)
				{
					fulcrumIds.Add(c.gameObject.GetInstanceID());
					foreach (var m in partsDictionary.puppetMaster.muscles)
					{
						if (m.colliders.Contains(c))
						{
							fulcrumMuscles.Add(m);
						}
					}
				}
				if (contactsInCollider >= minContactsToCountFulcrumIn && contactsInCollider <= maxContactsToCountFulcrumIn)
				{
					foreach (var m in partsDictionary.puppetMaster.muscles)
					{
						if (m.colliders.Contains(c))
						{
							fulcrumMusclesFilteredByNumOfContacts.Add(m);
						}
					}
				}
			}
		}
		else
		{
			Debug.LogWarning("Слишком мало коллайдеров. Точки опоры не будут вычислены.");
		}
	}

	private void FindIfAnimationFulcrumsAreSatisfied()
	{
		bool animationCurvesExist = false;

		if (useLegacyFulcrumsCurves)
		{
			animationCurvesExist = false;
			if (partsDictionary.characterAnimator.GetCurrentAnimatorStateInfo(0).IsName(actionsAnimStateName) &&
			curvedActionsIds.Contains(Mathf.RoundToInt(partsDictionary.characterAnimator.GetFloat(actionsAnimProp))))
			{
				animationCurvesExist = true;
			}
			if (curvedAnimStatesNames.Any(a => partsDictionary.characterAnimator.GetCurrentAnimatorStateInfo(0).IsName(a)))
			{
				animationCurvesExist = true;
			}
		}
		else
		{
			animationCurvesExist = false;
		}

		HashSet<Collider> fulcrumsFromSettings = new HashSet<Collider>();

		if (animationCurvesExist)
		{
			foreach (var c in Colliders)
			{
				if (partsDictionary.characterAnimator.GetFloat(partsDictionary.GetAnimatorPartNameFromBodyPartName(c.name)) > 0.5)
				{
					fulcrumsFromSettings.Add(c);
				}
			}
		}

		foreach (var c in Colliders)
		{
			if (AnimationSettingsSystem.ASS.GetCompiledSettings(partsDictionary.characterAnimator, 0).Func_self.Fulcrums.Any(f => f.ToString() == c.name)) // TODO: магическое число
			{
				fulcrumsFromSettings.Add(c);
			}
		}


		List<Tuple<Vector3, string>> animationFulcrums3D = new List<Tuple<Vector3, string>>();

		foreach (var c in fulcrumsFromSettings)
		{
			Collider col = Colliders.Find(x => x.name == c.name);
			if (col == null)
			{
				Debug.LogWarning($"Balance Watcher не смог найти часть тела с именем {c.name}. Не все точки опоры будут найдены.");
			}
			else
			{
				animationFulcrums3D.Add(new Tuple<Vector3, string>(col.ClosestPoint(col.gameObject.transform.position + Vector3.down * 10), col.name));
			}
		}

		animationHaveGround = animationFulcrums3D.Count() > 0;
		float maxOffset = -1000000;
		maxFulcrumDissatisfactionBodyPartName = "";

		foreach (var af in animationFulcrums3D)
		{
			RaycastHit hit;
			if (Physics.Raycast(af.Item1 + Vector3.up * 0.5f, Vector3.down, out hit, 10, layersToStandOn))
			{
				float dist = hit.distance - 0.5f;
				if (dist > maxOffset)
				{
					maxOffset = dist;
					maxFulcrumDissatisfactionBodyPartName = af.Item2;
				}
			}
		}

		maxFulcrumDissatisfactionOffset = maxOffset;

	}
	#endregion

	#region legs
	private void UpdateLeadLeg()
	{
		float left = Vector3.Dot(partsDictionary.characterTransform.forward, GetRigidbodyFromBodyPart(BodyPart.LeftFoot).transform.position);
		float right = Vector3.Dot(partsDictionary.characterTransform.forward, GetRigidbodyFromBodyPart(BodyPart.RightFoot).transform.position);
		if (left > right) LeadLeg = LeftRight.Left; else LeadLeg = LeftRight.Right;
	}
	#endregion

	#region raycasts
	private void UpdateDistanceToGround()
	{
		Ray ray = new Ray(partsDictionary.characterTransform.position + Vector3.up, Vector3.down);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, 100000, layersToStandOn))
		{
			DistanceToGround = hit.distance - 1;
		}
		else
		{
			DistanceToGround = Mathf.Infinity;
		}

		ray = new Ray(partsDictionary.characterTransform.position + Vector3.up
						+ partsDictionary.characterTransform.forward * distanceToGroundOffsetForward, Vector3.down);
		if (Physics.Raycast(ray, out hit, 100000, layersToStandOn))
		{
			OffsettedDistanceToGround = hit.distance - 1;
		}
		else
		{
			OffsettedDistanceToGround = Mathf.Infinity;
		}
	}
	#endregion

	#region misc
	private void FillDebugData()
	{
		debugMass = mass;
		debugCenter = massCenter;
		debugProcessedContacts = contactsProcessed;
		debugMassCenterProcessed = massCenterProcessed;
		debugMassCenterInsideConvexHull = massCenterInsideConvexHull;
		debugDistanceBetweenMassCenterAndConvexHull = distanceBwMassCenterAndConvex;
		debugFallDirection = fallDirection;
		debugMinIntersection = minIntersectionProceed;
		debugMaxIntersection = maxIntersectionProceed;
	}
	#endregion

	#region unused
	private void DetermineFallDirection()
	{
		if (convexHullPoints.Count == 0 || massCenterInsideConvexHull)
		{
			fallDirection = MoveDirection.None;
			return;
		}
		Vector2 convexHullCenter = new Vector2(0, 0);
		foreach (var chp in convexHullPoints)
		{
			convexHullCenter += chp;
		}
		convexHullCenter /= convexHullPoints.Count;

		float absFall = Vector2.SignedAngle(Vector2.up, massCenterProcessed - convexHullCenter) + 90;
		float absForw = partsDictionary.characterTransform.rotation.eulerAngles.y;
		float locFall = absFall - absForw;

		while (locFall < -180) locFall += 360;
		while (locFall > 180) locFall -= 360;

		fallDirection =
			locFall < -135 ? MoveDirection.Down :
			locFall < -45 ? MoveDirection.Left :
			locFall < 45 ? MoveDirection.Up :
			locFall < 135 ? MoveDirection.Right :
			MoveDirection.Down;
	}

	private void FindDistanceToConvexHull()
	{
		if (massCenterInsideConvexHull)
		{
			closestPointToMassCenterOnConvex = massCenterProcessed;
			distanceBwMassCenterAndConvex = 0;
			return;
		}

		distanceBwMassCenterAndConvex = float.MaxValue;
		for (int i = 0; i < convexHullPoints.Count - 1; i++)
		{
			float a1 = Vector2.Angle(convexHullPoints[i + 1] - convexHullPoints[i], massCenterProcessed - convexHullPoints[i]);
			if (a1 > 90)
			{
				TryPointAsDistance(convexHullPoints[i]);
				continue;
			}
			float a2 = Vector2.Angle(convexHullPoints[i] - convexHullPoints[i + 1], massCenterProcessed - convexHullPoints[i + 1]);
			if (a2 > 90)
			{
				TryPointAsDistance(convexHullPoints[i + 1]);
				continue;
			}
			Vector2 lbase = (convexHullPoints[i + 1] - convexHullPoints[i]).normalized;
			float proj = Vector2.Dot(massCenterProcessed - convexHullPoints[i], lbase);
			Vector2 closest = convexHullPoints[i] + lbase * proj;
			TryPointAsDistance(closest);
		}
	}

	private void TryPointAsDistance(Vector2 p)
	{
		if ((p - massCenterProcessed).magnitude < distanceBwMassCenterAndConvex)
		{
			distanceBwMassCenterAndConvex = (p - massCenterProcessed).magnitude;
			closestPointToMassCenterOnConvex = p;
		}
	}

	private void CheckMassCenterInsideConvexHull()
	{
		if (convexHullPoints.Count < 3)
			massCenterInsideConvexHull = false;
		else
		{
			massCenterInsideConvexHull = false;
			for (int i = 2; i < convexHullPoints.Count; i++)
			{
				if (PointInTriangle(massCenterProcessed, convexHullPoints[0], convexHullPoints[i - 1], convexHullPoints[i]))
				{
					massCenterInsideConvexHull = true;
					return;
				}
			}
		}
	}
	#endregion
}


class Vec2Cmprr : IComparer<Vector2>
{
	// http://e-maxx.ru/algo/convex_hull_graham
	public int Compare(Vector2 a, Vector2 b)
	{
		return (a.x < b.x || a.x == b.x && a.y < b.y) ? 1 : -1;
	}
}