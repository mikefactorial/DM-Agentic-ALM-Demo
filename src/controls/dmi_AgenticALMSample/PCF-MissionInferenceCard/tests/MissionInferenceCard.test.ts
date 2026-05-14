import { MissionInferenceCard } from "../MissionInferenceCard/index";

const PRIORITY_HIGH = 100000000;
const PRIORITY_MEDIUM = 100000001;
const PRIORITY_LOW = 100000002;

function makeContext(
    intent: string,
    priority: number | null,
    actions: string
): ComponentFramework.Context<any> {
    return {
        parameters: {
            intent: { raw: intent },
            priority: { raw: priority },
            actions: { raw: actions },
        },
    } as any;
}

describe("MissionInferenceCard", () => {
    let control: MissionInferenceCard;
    let container: HTMLDivElement;

    beforeEach(() => {
        control = new MissionInferenceCard();
        container = document.createElement("div");
        control.init({} as any, jest.fn(), {} as any, container);
    });

    // -----------------------------------------------------------------------
    // init
    // -----------------------------------------------------------------------

    describe("init", () => {
        it("appends a .mic-card element to the container", () => {
            expect(container.querySelector(".mic-card")).not.toBeNull();
        });
    });

    // -----------------------------------------------------------------------
    // updateView — empty state
    // -----------------------------------------------------------------------

    describe("updateView — empty state", () => {
        it("shows the awaiting message when both intent and actions are empty", () => {
            control.updateView(makeContext("", null, ""));
            const empty = container.querySelector(".mic-empty");
            expect(empty).not.toBeNull();
            expect(empty!.textContent).toBe("Awaiting signal analysis\u2026");
        });

        it("does not render a priority badge in the empty state", () => {
            control.updateView(makeContext("", null, ""));
            expect(container.querySelector(".mic-priority-badge")).toBeNull();
        });
    });

    // -----------------------------------------------------------------------
    // updateView — priority badge
    // -----------------------------------------------------------------------

    describe("updateView — priority badge", () => {
        it.each([
            [PRIORITY_HIGH,   "HIGH",    "high"],
            [PRIORITY_MEDIUM, "MEDIUM",  "medium"],
            [PRIORITY_LOW,    "LOW",     "low"],
            [null,            "UNKNOWN", "unknown"],
        ])(
            "renders '%s Priority' badge with class mic-priority-%s for value %s",
            (value, label, cssClass) => {
                control.updateView(makeContext("Some intent", value, ""));
                const badge = container.querySelector(".mic-priority-badge");
                expect(badge).not.toBeNull();
                expect(badge!.textContent).toBe(`${label} Priority`);
                expect(badge!.classList.contains(`mic-priority-${cssClass}`)).toBe(true);
            }
        );
    });

    // -----------------------------------------------------------------------
    // updateView — header
    // -----------------------------------------------------------------------

    describe("updateView — header", () => {
        it("renders the FIRST CONTACT ANALYSIS header when there is content", () => {
            control.updateView(makeContext("Peaceful contact", PRIORITY_LOW, ""));
            const header = container.querySelector(".mic-header");
            expect(header).not.toBeNull();
            expect(header!.textContent).toBe("FIRST CONTACT ANALYSIS");
        });
    });

    // -----------------------------------------------------------------------
    // updateView — intent
    // -----------------------------------------------------------------------

    describe("updateView — intent", () => {
        it("renders the intent value in a .mic-value span", () => {
            control.updateView(makeContext("Peaceful first contact", PRIORITY_MEDIUM, ""));
            const value = container.querySelector(".mic-value");
            expect(value).not.toBeNull();
            expect(value!.textContent).toBe("Peaceful first contact");
        });

        it("labels the intent row with SIGNAL INTENT", () => {
            control.updateView(makeContext("Trade negotiation", null, ""));
            const label = container.querySelector(".mic-label");
            expect(label!.textContent).toBe("SIGNAL INTENT");
        });

        it("escapes < > in intent to prevent script injection", () => {
            control.updateView(makeContext('<script>alert("xss")</script>', null, ""));
            const value = container.querySelector(".mic-value");
            // The serialized HTML must not contain a raw <script> tag
            expect(value!.innerHTML).not.toContain("<script>");
            // The angle brackets must be escaped
            expect(value!.innerHTML).toContain("&lt;");
            expect(value!.innerHTML).toContain("&gt;");
            // The original text should still be readable as text content
            expect(value!.textContent).toContain("alert");
        });

        it("escapes & in intent text", () => {
            control.updateView(makeContext("Alpha & Beta", null, ""));
            const value = container.querySelector(".mic-value");
            expect(value!.innerHTML).toContain("&amp;");
        });

        it("does not render an intent row when intent is empty", () => {
            control.updateView(makeContext("", null, "Action one"));
            expect(container.querySelector(".mic-value")).toBeNull();
        });
    });

    // -----------------------------------------------------------------------
    // updateView — actions
    // -----------------------------------------------------------------------

    describe("updateView — actions", () => {
        it("renders semicolon-separated actions as individual list items", () => {
            control.updateView(makeContext("", null, "Establish comm link; Scan for life signs; Prepare greeting"));
            const items = container.querySelectorAll(".mic-action-list li");
            expect(items).toHaveLength(3);
            expect(items[0].textContent).toBe("Establish comm link");
            expect(items[1].textContent).toBe("Scan for life signs");
            expect(items[2].textContent).toBe("Prepare greeting");
        });

        it("trims whitespace around each action", () => {
            control.updateView(makeContext("", null, "  Action one  ;  Action two  "));
            const items = container.querySelectorAll(".mic-action-list li");
            expect(items[0].textContent).toBe("Action one");
            expect(items[1].textContent).toBe("Action two");
        });

        it("ignores empty segments caused by consecutive semicolons", () => {
            control.updateView(makeContext("", null, "Action one;;Action two"));
            const items = container.querySelectorAll(".mic-action-list li");
            expect(items).toHaveLength(2);
        });

        it("renders a single action without a semicolon as one list item", () => {
            control.updateView(makeContext("", null, "Single action"));
            const items = container.querySelectorAll(".mic-action-list li");
            expect(items).toHaveLength(1);
            expect(items[0].textContent).toBe("Single action");
        });

        it("does not render the actions section when actions is empty", () => {
            control.updateView(makeContext("Some intent", null, ""));
            expect(container.querySelector(".mic-actions-section")).toBeNull();
        });
    });

    // -----------------------------------------------------------------------
    // updateView — re-render clears previous state
    // -----------------------------------------------------------------------

    describe("updateView — re-render", () => {
        it("clears the previous render before drawing new content", () => {
            control.updateView(makeContext("First intent", PRIORITY_HIGH, "Do something"));
            control.updateView(makeContext("", null, ""));
            // Second render is the empty state — old content should be gone
            expect(container.querySelector(".mic-header")).toBeNull();
            expect(container.querySelector(".mic-empty")).not.toBeNull();
        });
    });

    // -----------------------------------------------------------------------
    // getOutputs / destroy
    // -----------------------------------------------------------------------

    describe("getOutputs", () => {
        it("returns an empty object", () => {
            expect(control.getOutputs()).toEqual({});
        });
    });

    describe("destroy", () => {
        it("does not throw", () => {
            expect(() => control.destroy()).not.toThrow();
        });
    });
});
