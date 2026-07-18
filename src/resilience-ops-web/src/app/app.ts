import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';

import { HealthResponse } from './core/models/health-response';
import { HealthApiService } from './core/services/health-api.service';

@Component({
  selector: 'app-root',
  imports: [DatePipe],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly healthApiService = inject(HealthApiService);

  protected readonly health = signal<HealthResponse | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected loadHealth(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.healthApiService.getHealth().subscribe({
      next: (response) => {
        this.health.set(response);
        this.isLoading.set(false);
      },
      error: () => {
        this.health.set(null);
        this.errorMessage.set(
          'Backendens status kunne ikke hentes. Kontrollér, at API’et kører.',
        );
        this.isLoading.set(false);
      },
    });
  }
}