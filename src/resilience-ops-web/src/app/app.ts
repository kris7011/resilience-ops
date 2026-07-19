import { DatePipe } from '@angular/common';
import {
  Component,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import {
  finalize,
  forkJoin,
} from 'rxjs';

import { HealthResponse } from './core/models/health-response';
import {
  Risk,
  riskSeverities,
  RiskSeverity,
  RiskStatus,
} from './core/models/risk';
import { HealthApiService } from './core/services/health-api.service';
import { RiskApiService } from './core/services/risk-api.service';

@Component({
  selector: 'app-root',
  imports: [
    DatePipe,
    ReactiveFormsModule,
  ],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly formBuilder =
    inject(FormBuilder);

  private readonly healthApiService =
    inject(HealthApiService);

  private readonly riskApiService =
    inject(RiskApiService);

  protected readonly health =
    signal<HealthResponse | null>(null);

  protected readonly risks =
    signal<Risk[]>([]);

  protected readonly isLoading =
    signal(false);

  protected readonly isSaving =
    signal(false);

  protected readonly updatingRiskId =
    signal<string | null>(null);

  protected readonly errorMessage =
    signal<string | null>(null);

  protected readonly severities =
    riskSeverities;

  protected readonly riskForm =
    this.formBuilder.nonNullable.group({
      title: [
        '',
        [
          Validators.required,
          Validators.maxLength(120),
        ],
      ],
      description: [
        '',
        [
          Validators.required,
          Validators.maxLength(1000),
        ],
      ],
      severity: [
        'Medium' as RiskSeverity,
        Validators.required,
      ],
    });

  public ngOnInit(): void {
    this.refreshDashboard();
  }

  protected refreshDashboard(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      health:
        this.healthApiService.getHealth(),
      risks:
        this.riskApiService.getRisks(),
    })
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        }),
      )
      .subscribe({
        next: ({ health, risks }) => {
          this.health.set(health);
          this.risks.set(risks);
        },
        error: () => {
          this.errorMessage.set(
            'The dashboard could not be loaded. Confirm that the API is running.',
          );
        },
      });
  }

  protected createRisk(): void {
    if (this.riskForm.invalid) {
      this.riskForm.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const request =
      this.riskForm.getRawValue();

    this.riskApiService
      .createRisk(request)
      .pipe(
        finalize(() => {
          this.isSaving.set(false);
        }),
      )
      .subscribe({
        next: (risk) => {
          this.risks.update(
            (currentRisks) => [
              risk,
              ...currentRisks,
            ],
          );

          this.riskForm.reset({
            title: '',
            description: '',
            severity: 'Medium',
          });
        },
        error: () => {
          this.errorMessage.set(
            'The risk could not be created.',
          );
        },
      });
  }

  protected changeRiskStatus(
    riskId: string,
    status: RiskStatus,
  ): void {
    if (this.updatingRiskId() !== null) {
      return;
    }

    this.updatingRiskId.set(riskId);
    this.errorMessage.set(null);

    this.riskApiService
      .updateRiskStatus(
        riskId,
        status,
      )
      .pipe(
        finalize(() => {
          this.updatingRiskId.set(null);
        }),
      )
      .subscribe({
        next: (updatedRisk) => {
          this.risks.update(
            (currentRisks) =>
              currentRisks.map((risk) =>
                risk.id === updatedRisk.id
                  ? updatedRisk
                  : risk,
              ),
          );
        },
        error: () => {
          this.errorMessage.set(
            'The risk status could not be updated.',
          );
        },
      });
  }
}