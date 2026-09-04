/// Deterministic pose-figure renderer: joint angles in, SVG string out.
///
/// No images, no randomness, no AI. The same angles always produce the same
/// picture, which is the whole point — a wrong-but-confident picture is how
/// patients learn the wrong movement.
///
/// The figure is a side-view schematic: a kinematic chain for the spine, one
/// arm, one leg, a head circle, an optional movement arrow, and a floor
/// line. Poses are authored as small angle sets (see App), never traced.
module Physio.Figures

type Pt = float * float

let private rad (deg : float) : float = deg * System.Math.PI / 180.0

let private r1 (v : float) : string = string (System.Math.Round(v, 1))

/// 2D kinematic chain. Each segment is (length, absolute angle in degrees:
/// 0 = up, clockwise positive, so 180 = straight down).
let chain (sx : float, sy : float) (segments : (float * float) list) : Pt list =
    let (_, rev) =
        List.fold
            (fun ((x, y), acc) (len, ang) ->
                let a = rad ang
                let p = (x + len * sin a, y - len * cos a)
                (p, p :: acc))
            ((sx, sy), [])
            segments
    (sx, sy) :: List.rev rev

/// A posed side-view figure: joint chains plus head circle.
type Pose =
    { Spine : Pt list
      Arm : Pt list
      Leg : Pt list
      HeadC : Pt
      HeadR : float }

/// Standing pose from three joint angles (degrees):
/// lean = whole-spine lean from vertical (+ = forward),
/// armHang = arm angle from straight-down hanging (0 = hangs, 90 = forward),
/// knee = knee bend (+ = heel swings back).
let standing (lean : float) (armHang : float) (knee : float) : Pose =
    let hip = (0.0, 0.0)
    // Hip -> mid-back -> shoulder -> neck base.
    let spine = chain hip [ (34.0, lean); (30.0, lean); (16.0, lean - 4.0) ]
    let shoulder = spine.[1]
    let neck = spine.[2]
    let headC = (fst neck + 9.0, snd neck - 13.0)
    // Shoulder -> elbow -> hand. Hanging is 180 (straight down).
    let arm = chain shoulder [ (26.0, 180.0 - armHang); (24.0, 184.0 - armHang) ]
    // Hip -> knee -> foot.
    let leg = chain hip [ (38.0, 178.0); (38.0, 178.0 + knee) ]
    { Spine = spine
      Arm = arm
      Leg = leg
      HeadC = headC
      HeadR = 11.0 }

type Arrow = { From : Pt; To : Pt }

let private pts (chain : Pt list) : string =
    chain
    |> List.map (fun (x, y) -> $"{r1 x},{r1 y}")
    |> String.concat " "

/// Full standalone SVG document for one pose. Bounds are computed from the
/// drawn points plus padding, so the figure can never crop itself.
let figureSvg (pose : Pose) (arrow : Arrow option) (label : string) : string =
    let extra =
        match arrow with
        | Some a -> [ a.From; a.To ]
        | None -> []
    let all = pose.Spine @ pose.Arm @ pose.Leg @ [ pose.HeadC ] @ extra
    let xs = all |> List.map fst
    let ys = all |> List.map snd
    let pad = 26.0
    let minX = List.min xs - pad
    let minY = List.min ys - pad - pose.HeadR
    let w = List.max xs - List.min xs + pad * 2.0 + pose.HeadR
    let h = List.max ys - List.min ys + pad * 2.0 + pose.HeadR * 2.0
    let (hx, hy) = pose.HeadC
    let ink = "#1f2937"
    let faint = "#cbd5e1"
    let brand = "#1f8ac9"
    let arrowSvg =
        match arrow with
        | Some a ->
            let (x1, y1) = a.From
            let (x2, y2) = a.To
            $"""<line x1="{r1 x1}" y1="{r1 y1}" x2="{r1 x2}" y2="{r1 y2}" stroke="{brand}" stroke-width="3.5" stroke-linecap="round" />
  <circle cx="{r1 x2}" cy="{r1 y2}" r="4.5" fill="{brand}" />"""
        | None -> ""
    $"""<svg viewBox="{r1 minX} {r1 minY} {r1 w} {r1 h}" role="img" aria-label="{label}" xmlns="http://www.w3.org/2000/svg">
  <title>{label}</title>
  <line x1="{r1 (minX + 6.0)}" y1="{r1 (List.max ys + 14.0)}" x2="{r1 (minX + w - 6.0)}" y2="{r1 (List.max ys + 14.0)}" stroke="{faint}" stroke-width="2" stroke-linecap="round" />
  <polyline points="{pts pose.Leg}" fill="none" stroke="{ink}" stroke-width="9" stroke-linecap="round" stroke-linejoin="round" />
  <polyline points="{pts pose.Spine}" fill="none" stroke="{ink}" stroke-width="9" stroke-linecap="round" stroke-linejoin="round" />
  <polyline points="{pts pose.Arm}" fill="none" stroke="{ink}" stroke-width="8" stroke-linecap="round" stroke-linejoin="round" />
  <circle cx="{r1 hx}" cy="{r1 hy}" r="{r1 pose.HeadR}" fill="none" stroke="{ink}" stroke-width="4" />
  {arrowSvg}
</svg>"""
