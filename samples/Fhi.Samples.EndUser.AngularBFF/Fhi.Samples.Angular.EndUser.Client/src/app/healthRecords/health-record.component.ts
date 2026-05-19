import {
  ChangeDetectionStrategy,
  Component,
  CUSTOM_ELEMENTS_SCHEMA,
  inject,
  signal,
} from '@angular/core';
import { HttpClient, HttpContext, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { SKIP_AUTH_REDIRECT } from '../interceptors/auth.interseptor';

interface Record {
  createdAt: string;
  name: string;
  description: string;
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}

@Component({
  selector: 'healthrecord-component',
  templateUrl: 'health-record.component.html',
  styleUrl: 'health-record.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class HealthRecordComponent {
  private readonly http = inject(HttpClient);

  /**
   * Records returned from the BFF.
   *   - `null`  → not yet requested (show "click to load" hint)
   *   - `[]`    → request completed, no records
   *   - `[...]` → records returned
   */
  protected readonly records = signal<Record[] | null>(null);

  /** Last error message, or `null` if the previous call succeeded. */
  protected readonly error = signal<string | null>(null);

  async getRecords(): Promise<void> {
    this.error.set(null);
    try {
      const response = await firstValueFrom(
        this.http.get<Record[]>('/bff/v1/health-records', {
          withCredentials: true,
          // Opt out of the global 401 → /login redirect so the user stays
          // in the SPA and sees the error banner instead of being bounced
          // through the OIDC flow on a transient failure.
          context: new HttpContext().set(SKIP_AUTH_REDIRECT, true),
        }),
      );
      this.records.set(response);
    } catch (err) {
      console.error('Failed to load health records', err);
      this.records.set(null);
      this.error.set(this.toMessage(err));
    }
  }

  private toMessage(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      const problem = err.error as ProblemDetails | string | null;
      if (problem && typeof problem === 'object') {
        return problem.detail ?? problem.title ?? `Request failed (${err.status}).`;
      }
      if (typeof problem === 'string' && problem.length > 0) {
        return problem;
      }
      if (err.status === 401) {
        return 'Your session has expired. Sign out and back in via /logout then /login.';
      }
      return `Request failed (${err.status} ${err.statusText}).`;
    }
    return 'Unexpected error loading health records.';
  }
}
