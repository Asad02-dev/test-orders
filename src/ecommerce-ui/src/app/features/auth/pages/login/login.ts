import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [],
  templateUrl: './login.html',
  styleUrl: './login.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private readonly oidcService = inject(OidcSecurityService);

  login(): void {
    this.oidcService.authorize();
  }
}
