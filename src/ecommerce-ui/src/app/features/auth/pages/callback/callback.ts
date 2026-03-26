import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'app-callback',
  standalone: true,
  template: `
    <div class="callback-loading">
      <div class="spinner"></div>
      <p>Completing sign in...</p>
    </div>
  `,
  styles: [`
    .callback-loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      gap: 1rem;
    }

    .spinner {
      width: 40px;
      height: 40px;
      border: 4px solid #f3f3f3;
      border-top: 4px solid #3498db;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
  `]
})
export class CallbackComponent implements OnInit {
  private readonly oidcService = inject(OidcSecurityService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.oidcService.checkAuth().subscribe(({ isAuthenticated }) => {
      if (isAuthenticated) {
        // Redirect to dashboard after successful authentication
        this.router.navigate(['/dashboard']);
      } else {
        // If authentication failed, redirect to login
        this.router.navigate(['/auth/login']);
      }
    });
  }
}
