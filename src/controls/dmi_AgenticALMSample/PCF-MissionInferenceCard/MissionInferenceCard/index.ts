import { IInputs, IOutputs } from "./generated/ManifestTypes";

const PRIORITY_HIGH = 100000000;
const PRIORITY_MEDIUM = 100000001;
const PRIORITY_LOW = 100000002;

export class MissionInferenceCard implements ComponentFramework.StandardControl<IInputs, IOutputs> {
    private _container: HTMLDivElement;
    private _card: HTMLDivElement;

    constructor() {
        // Empty
    }

    public init(
        context: ComponentFramework.Context<IInputs>,
        notifyOutputChanged: () => void,
        state: ComponentFramework.Dictionary,
        container: HTMLDivElement
    ): void {
        this._container = container;
        this._card = document.createElement("div");
        this._card.className = "mic-card";
        this._container.appendChild(this._card);
    }

    public updateView(context: ComponentFramework.Context<IInputs>): void {
        const intent = context.parameters.intent.raw ?? "";
        const priorityValue = context.parameters.priority.raw ?? null;
        const actions = context.parameters.actions.raw ?? "";

        this._card.innerHTML = "";

        if (!intent && !actions) {
            this._card.innerHTML = `<div class="mic-empty">Awaiting signal analysis…</div>`;
            return;
        }

        // Priority badge
        const { label, cssClass } = this._resolvePriority(priorityValue);
        const badge = document.createElement("div");
        badge.className = `mic-priority-badge mic-priority-${cssClass}`;
        badge.textContent = `${label} Priority`;
        this._card.appendChild(badge);

        // Header
        const header = document.createElement("div");
        header.className = "mic-header";
        header.textContent = "FIRST CONTACT ANALYSIS";
        this._card.appendChild(header);

        // Intent
        if (intent) {
            const intentRow = document.createElement("div");
            intentRow.className = "mic-row";
            intentRow.innerHTML = `<span class="mic-label">SIGNAL INTENT</span><span class="mic-value">${this._escape(intent)}</span>`;
            this._card.appendChild(intentRow);
        }

        // Actions
        if (actions) {
            const actionsSection = document.createElement("div");
            actionsSection.className = "mic-actions-section";
            actionsSection.innerHTML = `<div class="mic-label">RECOMMENDED ACTIONS</div>`;

            const actionList = document.createElement("ul");
            actionList.className = "mic-action-list";
            const actionItems = actions.split(";").map(a => a.trim()).filter(a => a.length > 0);
            actionItems.forEach(action => {
                const li = document.createElement("li");
                li.textContent = action;
                actionList.appendChild(li);
            });
            actionsSection.appendChild(actionList);
            this._card.appendChild(actionsSection);
        }
    }

    public getOutputs(): IOutputs {
        return {};
    }

    public destroy(): void {
        // Cleanup
    }

    private _resolvePriority(value: number | null): { label: string; cssClass: string } {
        switch (value) {
            case PRIORITY_HIGH:   return { label: "HIGH",   cssClass: "high" };
            case PRIORITY_MEDIUM: return { label: "MEDIUM", cssClass: "medium" };
            case PRIORITY_LOW:    return { label: "LOW",    cssClass: "low" };
            default:              return { label: "UNKNOWN", cssClass: "unknown" };
        }
    }

    private _escape(text: string): string {
        return text
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }
}

