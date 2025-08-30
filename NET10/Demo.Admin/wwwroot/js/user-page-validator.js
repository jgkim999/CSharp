/**
 * User 페이지 상태 검증 도구
 * 브라우저 개발자 도구에서 사용할 수 있는 상태 검증 함수들을 제공합니다.
 */
window.userPageStateValidator = {
    /**
     * 현재 User 페이지의 상태 정보를 가져옵니다
     * @returns {Promise<Object|null>} 현재 상태 정보 또는 null
     */
    getCurrentState: async function () {
        try {
            // Blazor 컴포넌트에서 상태 정보를 가져옵니다
            const state = await DotNet.invokeMethodAsync('Demo.Admin', 'GetCurrentStateInfo');
            console.log('🔍 현재 User 페이지 상태:', state);
            return state;
        } catch (error) {
            console.error('❌ 상태 정보 가져오기 실패:', error);
            return null;
        }
    },

    /**
     * LocalStorage의 상태를 확인합니다
     * @returns {Object} LocalStorage에 저장된 값들
     */
    checkLocalStorage: function () {
        const searchTerm = localStorage.getItem('UserSearchTerm');
        const pageSize = localStorage.getItem('PageSize');

        console.log('📦 LocalStorage 상태:');
        console.log('   - UserSearchTerm:', searchTerm);
        console.log('   - PageSize:', pageSize);

        return { searchTerm, pageSize };
    },

    /**
     * 전체 상태 일관성 검증을 실행합니다
     * @returns {Promise<boolean>} 검증 통과 여부
     */
    validateAll: async function () {
        console.log('=== 전체 상태 검증 시작 ===');

        const currentState = await this.getCurrentState();
        const localStorageState = this.checkLocalStorage();

        if (!currentState) {
            console.log('❌ 상태 검증 실패: 현재 상태를 가져올 수 없습니다.');
            return false;
        }

        // 상태 일관성 검증
        const validations = [
            {
                name: '검색어 일관성',
                valid: currentState.StoredSearchTerm === currentState.CurrentSearchTerm,
                details: `저장된값='${currentState.StoredSearchTerm}', 현재값='${currentState.CurrentSearchTerm}'`
            },
            {
                name: '입력필드 일관성',
                valid: currentState.SearchTerm === currentState.CurrentSearchTerm,
                details: `입력필드='${currentState.SearchTerm}', 현재검색어='${currentState.CurrentSearchTerm}'`
            },
            {
                name: '페이지크기 일관성',
                valid: currentState.StoredPageSize === 0 || currentState.StoredPageSize === currentState.PageSize,
                details: `저장된값=${currentState.StoredPageSize}, 현재값=${currentState.PageSize}`
            },
            {
                name: '데이터그리드 초기화',
                valid: currentState.DataGridExists,
                details: `그리드 존재: ${currentState.DataGridExists}`
            },
            {
                name: '자동검색 실행',
                valid: !currentState.CurrentSearchTerm || currentState.AutoSearchExecuted,
                details: `검색어존재: ${!!currentState.CurrentSearchTerm}, 자동검색실행: ${currentState.AutoSearchExecuted}`
            }
        ];

        console.log('📊 상태 일관성 검증 결과:');
        let passedCount = 0;
        validations.forEach(v => {
            const status = v.valid ? '✅' : '❌';
            console.log(`   ${status} ${v.name}: ${v.details}`);
            if (v.valid) passedCount++;
        });

        console.log(`📈 전체 검증 결과: ${passedCount}/${validations.length} 통과`);

        if (passedCount === validations.length) {
            console.log('🎉 모든 상태 일관성 검증이 통과되었습니다!');
            return true;
        } else {
            console.log('⚠️ 일부 상태 일관성 검증이 실패했습니다.');
            return false;
        }
    },

    /**
     * 특정 시나리오에 대한 테스트를 실행합니다
     * @param {string} scenario 테스트 시나리오 이름
     */
    runScenarioTest: async function (scenario) {
        console.log(`🧪 시나리오 테스트 시작: ${scenario}`);
        
        switch (scenario) {
            case 'page-refresh':
                await this.testPageRefreshScenario();
                break;
            case 'navigation':
                await this.testNavigationScenario();
                break;
            case 'empty-search':
                await this.testEmptySearchScenario();
                break;
            default:
                console.log('❌ 알 수 없는 시나리오입니다. 사용 가능한 시나리오: page-refresh, navigation, empty-search');
        }
    },

    /**
     * 페이지 새로고침 시나리오 테스트
     */
    testPageRefreshScenario: async function () {
        console.log('📄 페이지 새로고침 시나리오 테스트');
        console.log('1. 현재 상태를 확인합니다...');
        
        const beforeState = await this.getCurrentState();
        if (!beforeState) {
            console.log('❌ 테스트 실패: 현재 상태를 가져올 수 없습니다.');
            return;
        }

        console.log('2. 검색어가 있는지 확인합니다...');
        if (!beforeState.CurrentSearchTerm) {
            console.log('ℹ️ 검색어가 없습니다. 먼저 검색어를 입력하고 검색을 실행한 후 페이지를 새로고침하세요.');
            return;
        }

        console.log(`3. 현재 검색어: '${beforeState.CurrentSearchTerm}'`);
        console.log('4. 이제 페이지를 새로고침(F5 또는 Ctrl+R)하고 다시 이 함수를 실행하세요.');
        console.log('   새로고침 후: await userPageStateValidator.validatePageRefreshResult()');
    },

    /**
     * 페이지 새로고침 결과 검증
     */
    validatePageRefreshResult: async function () {
        console.log('🔄 페이지 새로고침 결과 검증');
        
        const afterState = await this.getCurrentState();
        if (!afterState) {
            console.log('❌ 검증 실패: 현재 상태를 가져올 수 없습니다.');
            return;
        }

        console.log('검증 항목:');
        console.log(`✅ 검색어 복원: '${afterState.SearchTerm}'`);
        console.log(`✅ 자동 검색 실행: ${afterState.AutoSearchExecuted}`);
        console.log(`✅ 데이터 그리드 상태: 총 ${afterState.DataGridTotalItems}개 항목`);
        
        await this.validateAll();
    },

    /**
     * 네비게이션 시나리오 테스트 안내
     */
    testNavigationScenario: async function () {
        console.log('🧭 네비게이션 시나리오 테스트');
        console.log('1. 현재 상태를 확인합니다...');
        
        const beforeState = await this.getCurrentState();
        if (!beforeState) {
            console.log('❌ 테스트 실패: 현재 상태를 가져올 수 없습니다.');
            return;
        }

        console.log(`2. 현재 검색어: '${beforeState.CurrentSearchTerm || '없음'}'`);
        console.log('3. 다른 페이지로 이동한 후 다시 User 페이지로 돌아와서 다음 함수를 실행하세요:');
        console.log('   await userPageStateValidator.validateNavigationResult()');
    },

    /**
     * 네비게이션 결과 검증
     */
    validateNavigationResult: async function () {
        console.log('🔙 네비게이션 결과 검증');
        await this.validateAll();
    },

    /**
     * 빈 검색어 시나리오 테스트 안내
     */
    testEmptySearchScenario: async function () {
        console.log('🔍 빈 검색어 시나리오 테스트');
        console.log('1. 검색 입력 필드를 비우고 검색 버튼을 클릭하세요.');
        console.log('2. 검색 실행 후 다음 함수를 실행하세요:');
        console.log('   await userPageStateValidator.validateEmptySearchResult()');
    },

    /**
     * 빈 검색어 결과 검증
     */
    validateEmptySearchResult: async function () {
        console.log('🔍 빈 검색어 결과 검증');
        
        const state = await this.getCurrentState();
        const localStorage = this.checkLocalStorage();
        
        if (!state) {
            console.log('❌ 검증 실패: 현재 상태를 가져올 수 없습니다.');
            return;
        }

        console.log('검증 항목:');
        console.log(`✅ 현재 검색어 비어있음: '${state.CurrentSearchTerm}' (비어있어야 함)`);
        console.log(`✅ LocalStorage에서 제거됨: '${localStorage.searchTerm}' (null이어야 함)`);
        console.log(`✅ 전체 목록 표시: 총 ${state.DataGridTotalItems}개 항목`);
        
        await this.validateAll();
    },

    /**
     * 도움말을 출력합니다
     */
    help: function () {
        console.log('🔧 User 페이지 상태 검증 도구 사용법:');
        console.log('');
        console.log('📋 기본 함수:');
        console.log('   - userPageStateValidator.getCurrentState(): 현재 상태 정보 조회');
        console.log('   - userPageStateValidator.checkLocalStorage(): LocalStorage 상태 확인');
        console.log('   - userPageStateValidator.validateAll(): 전체 상태 일관성 검증');
        console.log('');
        console.log('🧪 시나리오 테스트:');
        console.log('   - userPageStateValidator.runScenarioTest("page-refresh"): 페이지 새로고침 테스트');
        console.log('   - userPageStateValidator.runScenarioTest("navigation"): 네비게이션 테스트');
        console.log('   - userPageStateValidator.runScenarioTest("empty-search"): 빈 검색어 테스트');
        console.log('');
        console.log('💡 사용 예시:');
        console.log('   1. 페이지 로드 후: await userPageStateValidator.validateAll()');
        console.log('   2. 검색 실행 후: await userPageStateValidator.validateAll()');
        console.log('   3. 상태 확인: await userPageStateValidator.getCurrentState()');
        console.log('   4. 시나리오 테스트: await userPageStateValidator.runScenarioTest("page-refresh")');
    }
};

// 페이지 로드 시 도움말 표시
console.log('🔧 User 페이지 상태 검증 도구가 로드되었습니다.');
console.log('   사용법을 보려면: userPageStateValidator.help()');