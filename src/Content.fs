/// Seed content: one area (neck), two items, production-level copy.
///
/// STATUS: draft. Written carefully, but no clinician has reviewed a word of
/// it, so it stays Draft until one does.
module Physio.Content

open Physio.Domain

let areas : Area list =
    [ { Id = "neck"
        Name = "Neck"
        Lede = "Gentle mobility work for a stiff neck. Your physiotherapist may have given you one of these or both — follow only what they went through with you." } ]

let items : Item list =
    [ { Id = "ex-neck-01"
        AreaId = "neck"
        Section = Exercise
        Name = "Chin tuck"
        Start = "Sit tall in a firm chair with your back supported. Look straight ahead, shoulders relaxed and down."
        Movement = "Glide your chin straight back, as if making a double chin."
        Direction = "Straight back, keeping your eyes level — do not tip your head up or down."
        Return = "Hold, then relax your chin forward to the start."
        Dose = { HoldSeconds = Some 5; Reps = Some 10; Sets = Some 2; EachSide = false }
        Target = "Deep neck flexor muscles at the front of the neck."
        Safety = "Stop if you feel sharp pain, dizziness, or tingling down an arm."
        ImageAlt = "Person sitting tall, gliding the chin straight back with eyes level."
        Status = Draft }
      { Id = "str-neck-01"
        AreaId = "neck"
        Section = Stretching
        Name = "Chin-to-chest stretch"
        Start = "Sit tall in a firm chair with your back supported. Look straight ahead, shoulders relaxed and down."
        Movement = "Slowly lower your chin toward your chest until you feel a gentle stretch at the back of the neck."
        Direction = "Straight down — do not force it, and keep your shoulders still."
        Return = "Hold, then slowly lift your head back to the start."
        Dose = { HoldSeconds = Some 20; Reps = Some 3; Sets = Some 1; EachSide = false }
        Target = "Muscles and soft tissue at the back of the neck."
        Safety = "Stretch to gentle tension only — never into sharp pain, dizziness, or tingling."
        ImageAlt = "Person sitting tall, head bowed forward with chin toward the chest."
        Status = Draft } ]
