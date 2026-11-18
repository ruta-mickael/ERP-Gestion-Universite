import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideAward,
  lucideBookOpen,
  lucideCircleUser,
  lucideCreditCard,
  lucideGraduationCap,
  lucideLayoutDashboard,
  lucideReceipt,
  lucideUserCheck,
  lucideUserPlus,
  lucideUsers
} from '@ng-icons/lucide';
import { HlmSidebarImports } from '@spartan-ng/helm/sidebar';

@Component({
  selector: 'app-side-bar',
  standalone: true,
  imports: [HlmSidebarImports, NgIcon, RouterLink, RouterLinkActive],
  providers: [
    provideIcons({
      lucideGraduationCap,
      lucideLayoutDashboard,
      lucideUsers,
      lucideUserCheck,
      lucideBookOpen,
      lucideAward,
      lucideUserPlus,
      lucideCreditCard,
      lucideReceipt,
      lucideCircleUser
    })
  ],
  templateUrl: './side-bar.component.html'
})
export class SideBarComponent {}
