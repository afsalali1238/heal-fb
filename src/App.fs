/// Page assembly: content + specs + figures -> static HTML documents.
///
/// This module is the seam the static-site generator calls (see
/// scripts/render.mjs). It returns plain strings — no framework, no
/// runtime — so the patient gets static HTML with nothing to download
/// beyond it.
module Physio.App

open Physio.Domain
open Physio.Content
open Physio.Safety
open Physio.Figures
open Physio.Specs
open Physio.Checks

let private esc (s : string) : string =
    s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")

/// Build-time gate, re-exported so the render script fails the build on
/// any figure problem before writing a single file.
let validateAll () : GateReport = Checks.validateAll ()

let private figureFor (id : string) : Figure * Arrow =
    match specFor id with
    | None -> failwith $"No figure spec for item \"{id}\". Run the gate first."
    | Some spec ->
        let fig = buildFigure spec.End
        fig, arrowFor spec

let private optRow (label : string) (fmt : int -> string) (value : int option) : string =
    match value with
    | Some v -> $"""<div class="d"><dt>{label}</dt><dd>{fmt v}</dd></div>"""
    | None -> ""

let private itemCard (item : Item) : string =
    let fig, arrow = figureFor item.Id
    let art = figureSvg fig (Some arrow) item.ImageAlt
    let sectionName =
        match item.Section with
        | Stretching -> "Stretch"
        | Exercise -> "Exercise"
    let dose =
        optRow "Hold" (fun s -> $"{s} sec") item.Dose.HoldSeconds
        + optRow "Repeat" (fun r -> $"{r} times") item.Dose.Reps
        + optRow "Sets" string item.Dose.Sets
        + (if item.Dose.EachSide then
               """"<div class="d"><dt>Each side</dt><dd>Yes</dd></div>"""
           else
               "")
    $"""<article class="item" id="{item.Id}">
<div class="fig">{art}</div>
<div class="ibody">
<p class="chip">{sectionName}</p>
<h3>{esc item.Name}</h3>
<dl class="dose">{dose}</dl>
<dl class="steps">
<div><dt>Start</dt><dd>{esc item.Start}</dd></div>
<div><dt>Move</dt><dd>{esc item.Movement}</dd></div>
<div><dt>Direction</dt><dd>{esc item.Direction}</dd></div>
<div><dt>Return</dt><dd>{esc item.Return}</dd></div>
</dl>
<p class="target"><b>Target:</b> {esc item.Target}</p>
<p class="safety">{esc item.Safety}</p>
</div>
</article>"""

let private areaSection (area : Area) : string =
    let cards =
        itemsForArea area.Id |> List.map itemCard |> String.concat ""
    let count = itemsForArea area.Id |> List.length
    $"""<section class="area" id="area-{area.Id}">
<h2>{esc area.Name} <span class="count">{count}</span></h2>
<p class="lede">{esc area.Lede}</p>
{cards}
</section>"""

let private gateDraft () : string =
    let lis =
        triggers
        |> List.map (fun t -> $"""<li>{esc t.Label}</li>""")
        |> String.concat ""
    $"""<section class="gate">
<p class="step">Before you start</p>
<h2>Do any of these apply to you right now?</h2>
<ul>{lis}</ul>
<p class="gate-note">Answering nothing continues. Anything else — including “I’m not sure” — stops and shows a stop screen instead of exercises. (Static draft of the gate; the interactive version arrives with client scripting.)</p>
</section>"""

/// Page stylesheet. Plain string, deliberately NOT interpolated:
/// CSS braces must stay braces, so this never goes inside $"...".
let private pageCss = """
:root { color-scheme: light; --brand: #1f8ac9; --ink: #1f2937; --ink-2: #475569; --line: #e2e8f0; --bg: #ffffff; --soft: #f8fafc; --warn-bg: #fef3c7; --warn: #92400e; --go-bg: #dcfce7; --go: #15803d; }
body { margin: 0; background: var(--bg); color: var(--ink); font: 17px/1.55 system-ui, sans-serif; }
.wrap { max-width: 760px; margin: 0 auto; padding: 0 20px 64px; }
.banner { background: var(--warn-bg); color: var(--warn); text-align: center; font-size: 14px; padding: 10px 16px; }
header.top { background: var(--brand); color: #fff; padding: 14px 20px; }
header.top b { display: block; font-size: 12px; letter-spacing: .15em; font-weight: 500; opacity: .8; }
header.top span { font-size: 20px; font-weight: 700; }
.demoband { background: #eff6ff; color: #1d4ed8; border-bottom: 1px solid #bfdbfe; padding: 10px 20px; font-size: 14px; }
h1 { font-size: clamp(30px, 7vw, 40px); letter-spacing: -.02em; margin: 26px 0 8px; }
.lede { color: var(--ink-2); max-width: 56ch; }
.area h2 { font-size: 27px; letter-spacing: -.015em; margin: 34px 0 6px; padding-top: 22px; border-top: 1px solid var(--line); }
.count { font-size: 14px; color: var(--ink-2); font-weight: 400; }
.starthere { background: var(--go-bg); color: var(--go); border-radius: 12px; padding: 14px 17px; margin: 20px 0; }
.gate { border: 1px solid var(--line); border-radius: 14px; padding: 18px 20px; margin: 22px 0; }
.step { font-size: 12px; letter-spacing: .14em; text-transform: uppercase; color: var(--ink-2); margin: 0 0 6px; }
.gate h2 { margin: 0 0 10px; font-size: 24px; }
.gate ul { margin: 0; padding-left: 20px; display: grid; gap: 6px; }
.gate-note { color: var(--ink-2); font-size: 14.5px; }
.item { border: 1px solid var(--line); border-radius: 14px; overflow: hidden; margin: 0 0 18px; }
.fig { background: var(--soft); border-bottom: 1px solid var(--line); padding: 14px; }
.fig svg { width: 100%; max-height: 300px; display: block; }
.ibody { padding: 18px 20px 22px; display: grid; gap: 12px; }
.chip { display: inline-block; justify-self: start; margin: 0; font-size: 11px; font-weight: 600; letter-spacing: .09em; text-transform: uppercase; background: #e0f2fe; color: #0369a1; border-radius: 999px; padding: 3px 10px; }
.item h3 { margin: 0; font-size: 22px; }
.dose { display: flex; flex-wrap: wrap; gap: 1px; background: var(--line); border: 1px solid var(--line); border-radius: 11px; overflow: hidden; margin: 0; }
.dose .d { flex: 1 1 100px; background: var(--soft); padding: 10px 12px; }
.dose dt { font-size: 10px; letter-spacing: .12em; text-transform: uppercase; color: var(--ink-2); }
.dose dd { margin: 0; font-weight: 600; }
.steps { display: grid; gap: 8px; margin: 0; }
.steps div { display: grid; grid-template-columns: 74px 1fr; gap: 10px; }
.steps dt { font-size: 10.5px; letter-spacing: .11em; text-transform: uppercase; color: var(--ink-2); padding-top: 3px; }
.steps dd { margin: 0; }
.target { margin: 0; color: var(--ink-2); border-top: 1px solid var(--line); padding-top: 11px; }
.safety { margin: 0; background: var(--warn-bg); color: var(--warn); border-radius: 10px; padding: 11px 13px; font-size: 15px; }
"""

/// The whole demonstration library on one page: every area, every item.
/// Routing (one file per area) arrives as slice 3; this slice proves the
/// content model, specs, gate and renderer scale past the first area.
let renderPage () : string =
    let sections = areas |> List.map areaSection |> String.concat ""
    $"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>Patient Library (Fable demo)</title>
<style>{pageCss}</style>
</head>
<body>
<div class="banner">NOT MEDICAL ADVICE · STOP IF YOU FEEL SHARP PAIN</div>
<header class="top"><b>PHYSIOTHERAPY</b><span>Patient Library</span></header>
<div class="demoband">Fable demonstration build — content and figures are drafts awaiting clinician review. Not for patient use.</div>
<main class="wrap">
<h1>Exercises and stretches</h1>
<p class="lede">Follow only what your physiotherapist went through with you. Stop if you feel pain.</p>
{gateDraft ()}
<div class="starthere"><b>Start slowly</b> — perform only the movements your physiotherapist reviewed with you.</div>
{sections}
</main>
</body>
</html>"""
