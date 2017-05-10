/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Sebastian Stehle. All rights reserved
 */

import { Component, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';

import {
    AuthService,
    ComponentBase,
    ImmutableArray,
    NotificationService,
    Pager,
    UserDto,
    UserManagementService
} from 'shared';

@Component({
    selector: 'sqx-users-page',
    styleUrls: ['./users-page.component.scss'],
    templateUrl: './users-page.component.html'
})
export class UsersPageComponent extends ComponentBase implements OnInit {
    public currentUserId: string;

    public usersItems = ImmutableArray.empty<UserDto>();
    public usersPager = new Pager(0);
    public usersFilter = new FormControl();
    public usersQuery = '';

    constructor(notifications: NotificationService,
        private readonly userManagementService: UserManagementService,
        private readonly authService: AuthService
    ) {
        super(notifications);
    }

    public ngOnInit() {
        this.currentUserId = this.authService.user!.id;

        this.load();
    }

    public search() {
        this.usersPager = new Pager(0);
        this.usersQuery = this.usersFilter.value;

        this.load();
    }

    public load(showInfo = false) {
        this.userManagementService.getUsers(this.usersPager.pageSize, this.usersPager.skip, this.usersQuery)
            .subscribe(dtos => {
                this.usersItems = ImmutableArray.of(dtos.items);
                this.usersPager = this.usersPager.setCount(dtos.total);

                if (showInfo) {
                    this.notifyInfo('Users reloaded.');
                }
            }, error => {
                this.notifyError(error);
            });
    }

    public lock(id: string) {
        this.userManagementService.lockUser(id)
            .subscribe(() => {
                this.usersItems =
                    this.usersItems.replaceAll(
                        u => u.id === id,
                        u => new UserDto(u.id, u.email, u.displayName, u.pictureUrl, true));
            }, error => {
                this.notifyError(error);
            });
    }

    public unlock(id: string) {
        this.userManagementService.unlockUser(id)
            .subscribe(() => {
                this.usersItems =
                    this.usersItems.replaceAll(
                        u => u.id === id,
                        u => new UserDto(u.id, u.email, u.displayName, u.pictureUrl, false));
            }, error => {
                this.notifyError(error);
            });
    }

    public goNext() {
        this.usersPager = this.usersPager.goNext();

        this.load();
    }

    public goPrev() {
        this.usersPager = this.usersPager.goPrev();

        this.load();
    }
}

