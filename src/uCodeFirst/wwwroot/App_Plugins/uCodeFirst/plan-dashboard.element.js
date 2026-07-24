import { html, css, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';

// GET /umbraco/management/api/v1/code-first/plan — served by uCodeFirst.Api.PlanCodeFirstController.
// Same-origin fetch with credentials:'include' rides the backoffice's httpOnly auth cookie, the same
// way the backoffice's own generated API client authenticates (see @umbraco-cms/backoffice/http-client,
// which configures the shared client with credentials:'include' for exactly this reason).
const PLAN_ENDPOINT = '/umbraco/management/api/v1/code-first/plan';

/**
 * uCodeFirst backoffice dashboard — shows the current dry-run plan (creates, updates, prunes) computed
 * live from CodeFirstSyncService.ComputePlanAsync, and lets a user re-run it on demand. This is the only
 * way to see the plan without waiting for (or restarting) the app when uCodeFirst:Enabled is false.
 *
 * Scope note (mirrors the server-side caveat in CodeFirstSyncService.LogPlan): this preview covers
 * content types and media types only — data types, dictionary items, languages, and templates are not
 * yet covered by the plan/apply split and so are not shown here.
 */
class UCodeFirstPlanDashboardElement extends UmbLitElement {
	static properties = {
		_loading: { state: true },
		_error: { state: true },
		_plan: { state: true },
	};

	constructor() {
		super();
		this._loading = false;
		this._error = null;
		this._plan = null;
	}

	connectedCallback() {
		super.connectedCallback();
		this._loadPlan();
	}

	async _loadPlan() {
		this._loading = true;
		this._error = null;
		try {
			const response = await fetch(PLAN_ENDPOINT, {
				method: 'GET',
				credentials: 'include',
				headers: { Accept: 'application/json' },
			});

			if (!response.ok) {
				let detail = `${response.status} ${response.statusText}`;
				try {
					const problem = await response.json();
					if (problem && problem.detail) detail = problem.detail;
				} catch {
					// Response wasn't JSON — keep the status-text fallback.
				}
				throw new Error(detail);
			}

			this._plan = await response.json();
		} catch (err) {
			this._error = err instanceof Error ? err.message : String(err);
			this._plan = null;
		} finally {
			this._loading = false;
		}
	}

	render() {
		return html`
			<uui-box headline="uCodeFirst — Dry-run plan">
				<p>
					Live preview of the next <code>uCodeFirst</code> sync. Covers content types and media types
					only — data types, dictionary items, languages, and templates are not previewed yet.
				</p>

				${this._plan ? this._renderStatus(this._plan) : nothing}

				<div class="actions">
					<uui-button
						look="primary"
						label="Run dry-run now"
						?disabled=${this._loading}
						@click=${() => this._loadPlan()}>
						${this._loading ? html`<uui-loader-circle></uui-loader-circle>` : nothing}
						Run dry-run now
					</uui-button>
				</div>

				${this._error ? html`<uui-tag color="danger">${this._error}</uui-tag>` : nothing}
				${this._plan ? this._renderPlan(this._plan) : nothing}
				${!this._plan && !this._error && this._loading ? html`<uui-loader-circle></uui-loader-circle>` : nothing}
			</uui-box>
		`;
	}

	_renderStatus(plan) {
		const generated = new Date(plan.generatedAtUtc);
		const generatedLabel = Number.isNaN(generated.getTime())
			? plan.generatedAtUtc
			: generated.toLocaleString();

		return html`
			<div class="status-row">
				<uui-tag color=${plan.enabled ? 'warning' : 'positive'}>
					${plan.enabled ? 'Active mode (uCodeFirst:Enabled = true)' : 'Dry-run only (uCodeFirst:Enabled = false)'}
				</uui-tag>
				<uui-tag color="default">Strategy: ${plan.strategy}</uui-tag>
				<span class="generated-at">Generated: ${generatedLabel}</span>
			</div>
		`;
	}

	_renderPlan(plan) {
		const prunedProperties = plan.prunedProperties.map((p) => `${p.typeAlias}.${p.propertyAlias}`);
		const prunedGroups = plan.prunedGroups.map((g) => `${g.typeAlias}.${g.groupAlias}`);
		const pruningIsNonDestructive = plan.strategy === 'NonDestructive';

		return html`
			<uui-table>
				<uui-table-head>
					<uui-table-head-cell>Category</uui-table-head-cell>
					<uui-table-head-cell>Count</uui-table-head-cell>
					<uui-table-head-cell>Aliases</uui-table-head-cell>
				</uui-table-head>
				${this._renderRow('To create', plan.toCreate)}
				${this._renderRow('To update', plan.toUpdate)}
				${this._renderRow(
					'Pruned properties',
					prunedProperties,
					pruningIsNonDestructive ? 'N/A (Strategy = NonDestructive)' : undefined,
				)}
				${this._renderRow(
					'Pruned groups (now empty)',
					prunedGroups,
					pruningIsNonDestructive ? 'N/A (Strategy = NonDestructive)' : undefined,
				)}
			</uui-table>
		`;
	}

	_renderRow(label, items, overrideText) {
		const cellText = overrideText ?? (items.length ? items.join(', ') : '—');
		return html`
			<uui-table-row>
				<uui-table-cell>${label}</uui-table-cell>
				<uui-table-cell>${overrideText ? '—' : items.length}</uui-table-cell>
				<uui-table-cell>${cellText}</uui-table-cell>
			</uui-table-row>
		`;
	}

	static styles = css`
		:host {
			display: block;
		}

		p {
			max-width: 75ch;
		}

		.status-row {
			display: flex;
			align-items: center;
			gap: var(--uui-size-space-4, 12px);
			flex-wrap: wrap;
			margin-bottom: var(--uui-size-space-4, 12px);
		}

		.generated-at {
			color: var(--uui-color-text-alt, #6c6c6c);
			font-size: 0.85em;
		}

		.actions {
			margin: var(--uui-size-space-4, 12px) 0;
		}

		uui-table-cell:last-child {
			word-break: break-word;
		}
	`;
}

customElements.define('ucodefirst-plan-dashboard', UCodeFirstPlanDashboardElement);

export default UCodeFirstPlanDashboardElement;
